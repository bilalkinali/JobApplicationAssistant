import assert from "node:assert/strict";
import test from "node:test";
import {
  candidateFitBriefSections,
  hasCandidateFitBriefContent,
  parseCandidateFitBrief
} from "../dist-test/candidateFitBrief.js";

const fitBriefJson = JSON.stringify({
  candidateSummary: "Strong backend candidate for a regulated product team.",
  skillGroups: [
    {
      name: "Backend delivery",
      items: [
        {
          title: "C# APIs",
          summary: "Built production APIs.",
          supportingProfileFactIds: ["fact-1"]
        }
      ]
    }
  ],
  competencies: [
    {
      title: "Domain modelling",
      summary: "Keeps workflows explicit.",
      supportingProfileFactIds: ["fact-2"]
    }
  ],
  relevantProjects: [
    {
      title: "Application Assistant",
      summary: "Implemented evidence review flows.",
      supportingProfileFactIds: ["fact-3"]
    }
  ],
  transferableStrengths: [
    {
      title: "Product judgement",
      summary: "Can move between product and engineering concerns.",
      supportingProfileFactIds: ["fact-4"]
    }
  ],
  riskNotes: [
    {
      title: "Weak direct fintech proof",
      summary: "Mention as adjacent experience rather than deep domain ownership.",
      supportingProfileFactIds: ["fact-5"]
    }
  ]
});

test("candidate fit brief view model exposes all read-only summary groups", () => {
  const brief = parseCandidateFitBrief(fitBriefJson);

  assert.equal(hasCandidateFitBriefContent(brief), true);
  assert.equal(brief.candidateSummary, "Strong backend candidate for a regulated product team.");
  assert.equal(brief.skillGroups[0].name, "Backend delivery");
  assert.equal(brief.skillGroups[0].items[0].title, "C# APIs");

  const sections = candidateFitBriefSections(brief);
  assert.deepEqual(
    sections.map((section) => section.title),
    ["Competencies", "Relevant projects", "Transferable strengths", "Risk notes"]
  );
  assert.equal(sections.find((section) => section.key === "competencies").items[0].title, "Domain modelling");
  assert.equal(sections.find((section) => section.key === "relevantProjects").items[0].title, "Application Assistant");
  assert.equal(sections.find((section) => section.key === "transferableStrengths").items[0].title, "Product judgement");
  assert.equal(sections.find((section) => section.key === "riskNotes").tone, "risk");
  assert.equal(sections.find((section) => section.key === "riskNotes").items[0].title, "Weak direct fintech proof");
});

test("candidate fit brief rendering data does not expose profile fact ids as approved evidence", () => {
  const brief = parseCandidateFitBrief(fitBriefJson);

  assert.equal(brief.skillGroups[0].items[0].supportingProfileFactIds, undefined);
  assert.equal(candidateFitBriefSections(brief)[0].items[0].supportingProfileFactIds, undefined);
});

test("empty or malformed candidate fit brief is hidden", () => {
  assert.equal(hasCandidateFitBriefContent(parseCandidateFitBrief("{}")), false);
  assert.equal(hasCandidateFitBriefContent(parseCandidateFitBrief("not-json")), false);
});
