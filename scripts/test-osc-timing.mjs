import assert from "node:assert/strict";
import fs from "node:fs";

// Execute the timing expressions from the graphs, not a second copy of their formulas.
function expression(text) {
  text = text.trim();
  if (text.startsWith("(") && text.endsWith(")")) return expression(text.slice(1, -1));
  if (text.startsWith("if ")) {
    let depth = 0, thenAt = -1, elseAt = -1;
    for (let i = 3; i < text.length; i++) {
      if (text[i] === "(") depth++;
      if (text[i] === ")") depth--;
      if (depth === 0 && text.startsWith(" then ", i)) thenAt = i;
      if (depth === 0 && text.startsWith(" else ", i)) { elseAt = i; break; }
    }
    assert(thenAt > 0 && elseAt > thenAt, `Unsupported expression: ${text}`);
    return `(${expression(text.slice(3, thenAt))} ? ${expression(text.slice(thenAt + 6, elseAt))} : ${expression(text.slice(elseAt + 6))})`;
  }
  return text.replace(/ValueMax\(A=([^,]+), B=([^\)]+)\)/g, "Math.max($1, $2)");
}
function evaluate(text, state) {
  return Function(...Object.keys(state), `return ${expression(text)}`)(...Object.values(state));
}
function assignment(graph, name, marker) {
  const values = [...graph.matchAll(new RegExp(`\\b${name} <- ([^;\\n]+);`, "g"))].map(m => m[1]);
  const found = values.filter(value => value.includes(marker));
  assert.equal(found.length, 1, `${name} must have one ${marker} assignment`);
  return found[0];
}

for (const plugin of ["JoyCon", "Haptira"]) {
  const graph = fs.readFileSync(new URL(`../Flux/BYOHaptics${plugin}OSCOutput.pg`, import.meta.url), "utf8");
  assert(graph.includes("store StopTailRemaining: float;"));
  const countdown = assignment(graph, "StopTailRemaining", "DeltaTime");
  const enabled = assignment(graph, "SenderEnabled", "StopTailRemaining");
  const state = { StopTailRemaining: 0, ActiveState: true, DeltaTime: 0.01 };
  state.StopTailRemaining = evaluate(countdown, state);
  state.ActiveState = false;
  for (let i = 0; i < 25; i++) {
    assert.equal(evaluate(enabled, state), true, `${plugin}: shutdown must cover 250 ms`);
    state.StopTailRemaining = evaluate(countdown, state);
  }
  assert.equal(evaluate(enabled, state), false);
  state.ActiveState = true;
  assert.equal(evaluate(enabled, state), true, "Reactivation cancels shutdown");
  state.StopTailRemaining = evaluate(countdown, state);
  assert.equal(state.StopTailRemaining, 0.25);
}

const graph = fs.readFileSync(new URL("../Flux/BYOHapticsHaptiraOSCOutput.pg", import.meta.url), "utf8");
assert(graph.includes("store ResendTailRemaining: float;"));
assert(!graph.includes("DelayValue("));
const resend = assignment(graph, "SenderAutoResendInterval", "ResendTailRemaining");
const hold = assignment(graph, "ResendTailRemaining", "DeltaTime");
for (const duration of [0.01, 0.08, 0.5]) {
  const state = { AnyChannelOutput15: 1, ResendTailRemaining: 0, DeltaTime: 0.01, InactiveResendInterval: 1e9 };
  for (let frame = 0; frame < duration * 100; frame++) state.ResendTailRemaining = evaluate(hold, state);
  state.AnyChannelOutput15 = 0;
  for (let frame = 0; frame < 25; frame++) {
    assert.equal(evaluate(resend, state), 0.05, `${duration}s pulse: zero resend must be continuous`);
    state.ResendTailRemaining = evaluate(hold, state);
  }
  assert.equal(evaluate(resend, state), 1e9);
}

const routeMatches = graph.match(/RouteMatches = ([^;]+);/)[1];
const active = graph.match(/ActiveState = ([^;]+);/)[1];
const routeGate = graph.match(/If\(Condition=(if ConfigurationValid then StopTailRemaining[^,]+), OnTrue/)[1];
assert.equal([...graph.matchAll(/SenderURL <-/g)].length, 1, "No alternate route writer may bypass the tail");
const routeBlock = graph.slice(graph.indexOf(`If(Condition=${routeGate}`), graph.indexOf("DiagnosticsActive <-"));
for (const write of ["SenderURL <- EndpointCandidate", "AppliedEndpoint <- EndpointText", "AppliedPrefix <- ConfigChannelPrefix", "AppliedSuffix <- ConfigChannelSuffix", "Channel00Path <-", "Channel15Path <-"]) assert(routeBlock.includes(write));
for (const changedField of ["EndpointText", "ConfigChannelPrefix", "ConfigChannelSuffix"]) {
  const state = { AppliedEndpoint: "old", EndpointText: "old", AppliedPrefix: "/old/", ConfigChannelPrefix: "/old/", AppliedSuffix: "/value", ConfigChannelSuffix: "/value", ConfigurationValid: true, IsUnderLocalUser: true, Selected: true, StopTailRemaining: 0.25, DeltaTime: 0.01 };
  state[changedField] = "new";
  for (let frame = 0; frame < 25; frame++) {
    assert.equal(evaluate(routeGate, state), false, "Old route must not change during shutdown");
    state.RouteMatches = evaluate(routeMatches, state);
    state.ActiveState = evaluate(active, state);
    assert.equal(state.ActiveState, false, "New Bus values must not reach the old route");
    state.StopTailRemaining = evaluate(assignment(graph, "StopTailRemaining", "DeltaTime"), state);
  }
  assert.equal(evaluate(routeGate, state), true);
  state.AppliedEndpoint = state.EndpointText;
  state.AppliedPrefix = state.ConfigChannelPrefix;
  state.AppliedSuffix = state.ConfigChannelSuffix;
  state.RouteMatches = evaluate(routeMatches, state);
  assert.equal(evaluate(active, state), true);
  state.ConfigurationValid = false;
  assert.equal(evaluate(active, state), false);
  assert.equal(evaluate(routeGate, state), false);
}
console.log("OSC timing expressions: shutdown, short pulses, reactivation and route changes passed.");
