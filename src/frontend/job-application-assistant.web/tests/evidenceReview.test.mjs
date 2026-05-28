import assert from "node:assert/strict";
import test from "node:test";
import {
  canApproveEvidenceMatch,
  evidenceQualityPresentation,
  isRecommendedEvidence,
  isWeakEvidence,
  weakEvidenceReviewLabel
} from "../dist-test/evidenceReview.js";

test("evidence quality labels distinguish strong partial and weak matches", () => {
  assert.deepEqual(
    [
      evidenceQualityPresentation("Strong").label,
      evidenceQualityPresentation("Partial").label,
      evidenceQualityPresentation("Weak").label
    ],
    ["Strong match", "Partial match", "Weak match"]
  );

  assert.equal(evidenceQualityPresentation("Strong").cardClass, "quality-strong");
  assert.equal(evidenceQualityPresentation("Partial").cardClass, "quality-partial");
  assert.equal(evidenceQualityPresentation("Weak").cardClass, "quality-weak");
});

test("partial matches are usable but cautious evidence", () => {
  const presentation = evidenceQualityPresentation("Partial");

  assert.equal(presentation.tone, "pending");
  assert.match(presentation.guidance, /Usable with care/);
  assert.equal(canApproveEvidenceMatch({ id: "partial-match", quality: "Partial" }), true);
});

test("recommended evidence includes strong and partial matches only", () => {
  assert.equal(isRecommendedEvidence({ id: "strong-match", quality: "Strong" }), true);
  assert.equal(isRecommendedEvidence({ id: "partial-match", quality: "Partial" }), true);
  assert.equal(isRecommendedEvidence({ id: "weak-match", quality: "Weak" }), false);
  assert.equal(isRecommendedEvidence({ id: "unknown-match" }), false);
});

test("weak matches require deliberate review before approval", () => {
  const weakMatch = { id: "weak-match", quality: "Weak" };

  assert.equal(isWeakEvidence(weakMatch), true);
  assert.equal(canApproveEvidenceMatch(weakMatch), false);
  assert.equal(canApproveEvidenceMatch(weakMatch, true), true);
  assert.equal(weakEvidenceReviewLabel(false), "Review weak match");
  assert.equal(weakEvidenceReviewLabel(true), "Weak match reviewed");
});
