export type CandidateFitBrief = {
  candidateSummary: string;
  skillGroups: CandidateFitSkillGroup[];
  competencies: CandidateFitBriefItem[];
  relevantProjects: CandidateFitBriefItem[];
  transferableStrengths: CandidateFitBriefItem[];
  riskNotes: CandidateFitBriefItem[];
};

export type CandidateFitSkillGroup = {
  name: string;
  items: CandidateFitBriefItem[];
};

export type CandidateFitBriefItem = {
  title: string;
  summary: string;
  supportingProfileFactIds?: string[];
};

export type CandidateFitBriefSection = {
  key: "competencies" | "relevantProjects" | "transferableStrengths" | "riskNotes";
  title: string;
  items: CandidateFitBriefItem[];
  tone: "neutral" | "risk";
};

export function parseCandidateFitBrief(value: string | undefined): CandidateFitBrief | null {
  const parsed = parseJsonObject<Partial<CandidateFitBrief>>(value);
  if (!parsed) {
    return null;
  }

  return {
    candidateSummary: typeof parsed.candidateSummary === "string" ? parsed.candidateSummary : "",
    skillGroups: normalizeSkillGroups(parsed.skillGroups),
    competencies: normalizeItems(parsed.competencies),
    relevantProjects: normalizeItems(parsed.relevantProjects),
    transferableStrengths: normalizeItems(parsed.transferableStrengths),
    riskNotes: normalizeItems(parsed.riskNotes)
  };
}

export function hasCandidateFitBriefContent(brief: CandidateFitBrief | null): brief is CandidateFitBrief {
  return Boolean(
    brief &&
      (brief.candidateSummary.trim() ||
        brief.skillGroups.some((group) => group.items.length > 0) ||
        candidateFitBriefSections(brief).some((section) => section.items.length > 0))
  );
}

export function candidateFitBriefSections(brief: CandidateFitBrief): CandidateFitBriefSection[] {
  return [
    {
      key: "competencies",
      title: "Competencies",
      items: brief.competencies,
      tone: "neutral"
    },
    {
      key: "relevantProjects",
      title: "Relevant projects",
      items: brief.relevantProjects,
      tone: "neutral"
    },
    {
      key: "transferableStrengths",
      title: "Transferable strengths",
      items: brief.transferableStrengths,
      tone: "neutral"
    },
    {
      key: "riskNotes",
      title: "Risk notes",
      items: brief.riskNotes,
      tone: "risk"
    }
  ];
}

function normalizeSkillGroups(value: unknown): CandidateFitSkillGroup[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((group) => {
      if (!group || typeof group !== "object") {
        return null;
      }

      const candidate = group as Partial<CandidateFitSkillGroup>;
      const name = typeof candidate.name === "string" ? candidate.name.trim() : "";
      const items = normalizeItems(candidate.items);
      return name || items.length > 0 ? { name: name || "Skill group", items } : null;
    })
    .filter((group): group is CandidateFitSkillGroup => Boolean(group));
}

function normalizeItems(value: unknown): CandidateFitBriefItem[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }

      const candidate = item as Partial<CandidateFitBriefItem>;
      const title = typeof candidate.title === "string" ? candidate.title.trim() : "";
      const summary = typeof candidate.summary === "string" ? candidate.summary.trim() : "";
      return title || summary ? { title: title || "Untitled", summary } : null;
    })
    .filter((item): item is CandidateFitBriefItem => Boolean(item));
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
