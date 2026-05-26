export type EvidenceQuality = "Strong" | "Partial" | "Weak" | string;

export type EvidenceReviewMatch = {
  id: string;
  quality?: EvidenceQuality | null;
  reason?: string | null;
};

export type EvidenceQualityPresentation = {
  label: string;
  tone: "approved" | "pending" | "rejected" | "neutral";
  cardClass: string;
  guidance: string;
};

export function evidenceQualityPresentation(quality?: EvidenceQuality | null): EvidenceQualityPresentation {
  switch (normalizeEvidenceQuality(quality)) {
    case "strong":
      return {
        label: "Strong match",
        tone: "approved",
        cardClass: "quality-strong",
        guidance: "Good support for claims tied to this job signal."
      };
    case "partial":
      return {
        label: "Partial match",
        tone: "pending",
        cardClass: "quality-partial",
        guidance: "Usable with care. Treat this as cautious support, not full-strength proof."
      };
    case "weak":
      return {
        label: "Weak match",
        tone: "rejected",
        cardClass: "quality-weak",
        guidance: "Questionable support. Review deliberately and leave it out of approved proof unless stronger evidence is added."
      };
    default:
      return {
        label: "Unscored match",
        tone: "neutral",
        cardClass: "quality-unknown",
        guidance: "No quality score was provided for this match."
      };
  }
}

export function normalizeEvidenceQuality(quality?: EvidenceQuality | null): string {
  return typeof quality === "string" ? quality.trim().toLowerCase() : "";
}

export function isWeakEvidence(match: EvidenceReviewMatch): boolean {
  return normalizeEvidenceQuality(match.quality) === "weak";
}

export function isRecommendedEvidence(match: EvidenceReviewMatch): boolean {
  const quality = normalizeEvidenceQuality(match.quality);
  return quality === "strong" || quality === "partial";
}

export function canApproveEvidenceMatch(match: EvidenceReviewMatch, weakMatchReviewed = false): boolean {
  return !isWeakEvidence(match) || weakMatchReviewed;
}

export function weakEvidenceReviewLabel(isReviewed: boolean): string {
  return isReviewed ? "Weak match reviewed" : "Review weak match";
}
