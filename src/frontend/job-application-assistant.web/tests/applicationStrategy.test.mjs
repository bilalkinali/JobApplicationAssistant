import assert from "node:assert/strict";
import test from "node:test";
import {
  applicationStrategySections,
  hasApplicationStrategyContent,
  parseApplicationStrategy
} from "../dist-test/applicationStrategy.js";

const strategyJson = JSON.stringify({
  primaryAngles: [
    {
      title: "Backend delivery",
      rationale: "Lead with approved API evidence.",
      evidenceIds: ["match-api"],
      profileFactIds: ["fact-api"]
    }
  ],
  secondaryAngles: [
    {
      title: "Product judgement",
      rationale: "Use as supporting context.",
      evidenceIds: ["match-product"],
      profileFactIds: ["fact-product"]
    }
  ],
  gapHandlingGuidance: [
    {
      unmatchedRequirementId: "gap-kubernetes",
      guidance: "Mention Kubernetes as a learning interest only."
    },
    {
      unmatchedRequirementId: "gap-kubernetes-duplicate",
      guidance: "  Mention Kubernetes as a learning interest only.  "
    }
  ],
  claimsToAvoid: [
    {
      claim: "Led Kubernetes operations",
      reason: "Only weak evidence was approved."
    }
  ],
  toneGuidance: "Specific and cautious.",
  draftOutline: [
    {
      section: "Opening",
      guidance: "Connect backend delivery to the role.",
      evidenceIds: ["match-api"],
      profileFactIds: ["fact-api"]
    }
  ]
});

test("application strategy summary exposes compact read-only draft guidance", () => {
  const strategy = parseApplicationStrategy(strategyJson);

  assert.equal(hasApplicationStrategyContent(strategy), true);
  assert.ok(strategy);
  assert.equal(strategy.primaryAngles[0].title, "Backend delivery");
  assert.equal(strategy.primaryAngles[0].evidenceIds.length, 0);

  const sections = applicationStrategySections(strategy);
  assert.deepEqual(
    sections.map((section) => section.title),
    ["Primary angles", "Gap guidance", "Claims to avoid", "Draft outline"]
  );
  assert.match(sections.find((section) => section.key === "primaryAngles").items[0], /Backend delivery/);
  assert.deepEqual(sections.find((section) => section.key === "gapHandlingGuidance").items, [
    "Mention Kubernetes as a learning interest only."
  ]);
  assert.equal(sections.find((section) => section.key === "claimsToAvoid").tone, "risk");
  assert.match(sections.find((section) => section.key === "draftOutline").items[0], /Opening/);
});

test("empty or malformed application strategy is hidden", () => {
  assert.equal(hasApplicationStrategyContent(parseApplicationStrategy("{}")), false);
  assert.equal(hasApplicationStrategyContent(parseApplicationStrategy("not-json")), false);
});
