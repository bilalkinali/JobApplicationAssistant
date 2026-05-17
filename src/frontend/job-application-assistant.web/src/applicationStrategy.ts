export type ApplicationStrategy = {
  primaryAngles: ApplicationStrategyAngle[];
  secondaryAngles: ApplicationStrategyAngle[];
  gapHandlingGuidance: ApplicationStrategyGapGuidance[];
  claimsToAvoid: ApplicationStrategyClaimToAvoid[];
  toneGuidance: string;
  draftOutline: ApplicationStrategyOutlineItem[];
};

export type ApplicationStrategyAngle = {
  title: string;
  rationale: string;
  evidenceIds: string[];
  profileFactIds: string[];
};

export type ApplicationStrategyGapGuidance = {
  unmatchedRequirementId: string;
  guidance: string;
};

export type ApplicationStrategyClaimToAvoid = {
  claim: string;
  reason: string;
};

export type ApplicationStrategyOutlineItem = {
  section: string;
  guidance: string;
  evidenceIds: string[];
  profileFactIds: string[];
};

export type ApplicationStrategySection = {
  key: "primaryAngles" | "gapHandlingGuidance" | "claimsToAvoid" | "draftOutline";
  title: string;
  items: string[];
  tone: "neutral" | "risk";
};

export function parseApplicationStrategy(value: string | undefined): ApplicationStrategy | null {
  const parsed = parseJsonObject<Partial<ApplicationStrategy>>(value);
  if (!parsed) {
    return null;
  }

  return {
    primaryAngles: normalizeAngles(parsed.primaryAngles),
    secondaryAngles: normalizeAngles(parsed.secondaryAngles),
    gapHandlingGuidance: normalizeGapGuidance(parsed.gapHandlingGuidance),
    claimsToAvoid: normalizeClaimsToAvoid(parsed.claimsToAvoid),
    toneGuidance: typeof parsed.toneGuidance === "string" ? parsed.toneGuidance.trim() : "",
    draftOutline: normalizeOutline(parsed.draftOutline)
  };
}

export function hasApplicationStrategyContent(strategy: ApplicationStrategy | null): strategy is ApplicationStrategy {
  return Boolean(
    strategy &&
      (strategy.primaryAngles.length > 0 ||
        strategy.gapHandlingGuidance.length > 0 ||
        strategy.claimsToAvoid.length > 0 ||
        strategy.draftOutline.length > 0)
  );
}

export function applicationStrategySections(strategy: ApplicationStrategy): ApplicationStrategySection[] {
  return [
    {
      key: "primaryAngles",
      title: "Primary angles",
      items: strategy.primaryAngles.map((angle) => formatWithDetail(angle.title, angle.rationale)),
      tone: "neutral"
    },
    {
      key: "gapHandlingGuidance",
      title: "Gap guidance",
      items: strategy.gapHandlingGuidance.map((gap) => gap.guidance),
      tone: "neutral"
    },
    {
      key: "claimsToAvoid",
      title: "Claims to avoid",
      items: strategy.claimsToAvoid.map((claim) => formatWithDetail(claim.claim, claim.reason)),
      tone: "risk"
    },
    {
      key: "draftOutline",
      title: "Draft outline",
      items: strategy.draftOutline.map((item) => formatWithDetail(item.section, item.guidance)),
      tone: "neutral"
    }
  ];
}

function normalizeAngles(value: unknown): ApplicationStrategyAngle[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }

      const candidate = item as Partial<ApplicationStrategyAngle>;
      const title = normalizeString(candidate.title);
      const rationale = normalizeString(candidate.rationale);
      if (!title && !rationale) {
        return null;
      }

      const angle: ApplicationStrategyAngle = {
        title: title || "Untitled angle",
        rationale,
        evidenceIds: [],
        profileFactIds: []
      };
      return angle;
    })
    .filter((item): item is ApplicationStrategyAngle => Boolean(item));
}

function normalizeGapGuidance(value: unknown): ApplicationStrategyGapGuidance[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }

      const candidate = item as Partial<ApplicationStrategyGapGuidance>;
      const guidance = normalizeString(candidate.guidance);
      return guidance
        ? {
            unmatchedRequirementId: normalizeString(candidate.unmatchedRequirementId),
            guidance
          }
        : null;
    })
    .filter((item): item is ApplicationStrategyGapGuidance => Boolean(item));
}

function normalizeClaimsToAvoid(value: unknown): ApplicationStrategyClaimToAvoid[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }

      const candidate = item as Partial<ApplicationStrategyClaimToAvoid>;
      const claim = normalizeString(candidate.claim);
      const reason = normalizeString(candidate.reason);
      return claim || reason ? { claim: claim || "Unsupported claim", reason } : null;
    })
    .filter((item): item is ApplicationStrategyClaimToAvoid => Boolean(item));
}

function normalizeOutline(value: unknown): ApplicationStrategyOutlineItem[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }

      const candidate = item as Partial<ApplicationStrategyOutlineItem>;
      const section = normalizeString(candidate.section);
      const guidance = normalizeString(candidate.guidance);
      if (!section && !guidance) {
        return null;
      }

      const outlineItem: ApplicationStrategyOutlineItem = {
        section: section || "Draft section",
        guidance,
        evidenceIds: [],
        profileFactIds: []
      };
      return outlineItem;
    })
    .filter((item): item is ApplicationStrategyOutlineItem => Boolean(item));
}

function formatWithDetail(title: string, detail: string): string {
  return detail ? `${title}: ${detail}` : title;
}

function normalizeString(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

function parseJsonObject<T>(value: string | undefined): T | null {
  if (!value) {
    return null;
  }

  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed) ? parsed : null;
  } catch {
    return null;
  }
}
