import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiBaseUrl, apiDelete, apiGet, apiSend, apiSendForm } from "./api/client";
import { AppShell } from "./components/AppShell";
import {
  getAuditExportNotice,
  getCoverLetterExportState,
  getCoverLetterText
} from "./exportControls";
import {
  ErrorPresentation,
  formatError,
  plainError,
  technicalDetails
} from "./errorPresentation";
import {
  hasCandidateFitBriefContent,
  parseCandidateFitBrief
} from "./candidateFitBrief";
import type { CandidateFitBrief } from "./candidateFitBrief";
import {
  hasApplicationStrategyContent,
  parseApplicationStrategy
} from "./applicationStrategy";
import type { ApplicationStrategy } from "./applicationStrategy";
import {
  getDraftGenerationState,
  getEvidenceMatchingState,
  getGuidedNextAction,
  getJobAnalysisState,
  getPrepareApplicationPath,
  getProfileReadiness,
  isFakeProvider
} from "./readiness";
import { canApproveEvidenceMatch, isRecommendedEvidence } from "./evidenceReview";
import type {
  AiDiagnostics,
  AiProviderStatus,
  ApplicationForm,
  ApplicationSession,
  AssistedProfileImportResponse,
  ClaimAudit,
  CustomFact,
  CustomFactDraft,
  DraftQualityCheck,
  EvidenceMatch,
  GapDecision,
  GapDecisionValue,
  GeneratedDraft,
  GeneratedDraftForm,
  ImportedDraftFactBulkDecisionResponse,
  ImportedDraftFactDecisionResponse,
  ImportedDraftFactMergeResponse,
  ImportedDraftFactReviewQueue,
  ImportedDraftFactSplitResponse,
  InlineFeedback,
  JobSignalsDocument,
  PrepareApplicationResult,
  ProfileFact,
  ProfileFactForm,
  ProfileForm,
  UnmatchedRequirement,
  View
} from "./features/shared/types";
import { HomeView } from "./features/home/HomeView";
import { SettingsView } from "./features/applications/SettingsView";
import { ProfileFormPanel } from "./features/profile/ProfileFormPanel";
import { ProfileImportPanel } from "./features/profile/ProfileImportPanel";
import { ImportReviewQueues } from "./features/profile/ImportReviewQueues";
import { ProfileFactsPanel } from "./features/profile/ProfileFactsPanel";
import { ProfileFactEditor } from "./features/profile/ProfileFactEditor";
import { ApplicationListPanel } from "./features/applications/ApplicationListPanel";
import { ApplicationEditor } from "./features/applications/ApplicationEditor";
import { ApplicationWorkflowPanel } from "./features/applications/ApplicationWorkflowPanel";
import { ProviderReadinessSummary } from "./features/shared/summaries";
import "./styles.css";

const emptyProfile: ProfileForm = {
  fullName: "",
  email: "",
  phone: "",
  location: "",
  linkedInUrl: "",
  gitHubUrl: "",
  portfolioUrl: "",
  defaultLanguage: "English",
  danishTone: "",
  englishTone: ""
};
const emptyApplication: ApplicationForm = {
  companyName: "",
  roleTitle: "",
  applicationUrl: "",
  deadline: "",
  status: "Draft",
  jobPostingText: "",
  detectedLanguage: "",
  selectedLanguage: ""
};
const emptyProfileFact: ProfileFactForm = {
  type: "Experience",
  title: "",
  summary: "",
  status: "Draft",
  factItems: "[]",
  technologies: "[]",
  allowedClaims: "[]",
  forbiddenClaims: "[]"
};
const applicationStatuses = ["Draft", "PostingCaptured", "ReadyForReview", "PreparedForEvidenceReview", "Applied", "Archived"];
const auditReadinessOptions = ["All", "Current", "Stale", "Missing", "NotApplicable"];
const profileFactStatuses = ["Draft", "Approved", "Archived", "Rejected"];

function App() {
  const [view, setView] = useState<View>("home");
  const [profile, setProfile] = useState<ProfileForm>(emptyProfile);
  const [profileFacts, setProfileFacts] = useState<ProfileFact[]>([]);
  const [importReviewQueues, setImportReviewQueues] = useState<ImportedDraftFactReviewQueue[]>([]);
  const [selectedImportedDraftFactIds, setSelectedImportedDraftFactIds] = useState<string[]>([]);
  const [splitImportedDraftFacts, setSplitImportedDraftFacts] = useState("");
  const [profileImportFile, setProfileImportFile] = useState<File | null>(null);
  const [profileImportBusy, setProfileImportBusy] = useState(false);
  const [pendingImportReviewFocusId, setPendingImportReviewFocusId] = useState<string | null>(null);
  const [profileFactForm, setProfileFactForm] = useState<ProfileFactForm>(emptyProfileFact);
  const [selectedProfileFactId, setSelectedProfileFactId] = useState<string | null>(null);
  const [selectedProfileFactIds, setSelectedProfileFactIds] = useState<string[]>([]);
  const [applications, setApplications] = useState<ApplicationSession[]>([]);
  const [applicationForm, setApplicationForm] = useState<ApplicationForm>(emptyApplication);
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);
  const [applicationSearch, setApplicationSearch] = useState("");
  const [applicationStatusFilter, setApplicationStatusFilter] = useState("All");
  const [applicationReadinessFilter, setApplicationReadinessFilter] = useState("All");
  const [includeArchivedApplications, setIncludeArchivedApplications] = useState(false);
  const [approvedEvidenceDraft, setApprovedEvidenceDraft] = useState<EvidenceMatch[]>([]);
  const [reviewedWeakMatchIds, setReviewedWeakMatchIds] = useState<string[]>([]);
  const [gapDecisionsDraft, setGapDecisionsDraft] = useState<GapDecision[]>([]);
  const [isEvidenceReviewEditing, setIsEvidenceReviewEditing] = useState(false);
  const [expandedCustomFactRequirementId, setExpandedCustomFactRequirementId] = useState<string | null>(null);
  const [customFactDrafts, setCustomFactDrafts] = useState<Record<string, CustomFactDraft>>({});
  const [generatedDraftForm, setGeneratedDraftForm] = useState<GeneratedDraftForm>({
    coverLetterText: "",
    shortMotivationText: ""
  });
  const [workflowBusy, setWorkflowBusy] = useState<string | null>(null);
  const [exportBusy, setExportBusy] = useState<"txt" | "docx" | null>(null);
  const [exportFeedback, setExportFeedback] = useState<InlineFeedback | null>(null);
  const [profileImportFeedback, setProfileImportFeedback] = useState<InlineFeedback | null>(null);
  const [isClipboardAvailable, setIsClipboardAvailable] = useState(false);
  const [aiStatus, setAiStatus] = useState<AiProviderStatus | null>(null);
  const [aiDiagnostics, setAiDiagnostics] = useState<AiDiagnostics | null>(null);
  const [aiDiagnosticsError, setAiDiagnosticsError] = useState<ErrorPresentation | null>(null);
  const [aiDiagnosticsBusy, setAiDiagnosticsBusy] = useState(false);
  const [aiDiagnosticsLastRanAt, setAiDiagnosticsLastRanAt] = useState<string | null>(null);
  const [error, setError] = useState<ErrorPresentation | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const evidenceReviewRef = useRef<HTMLDivElement | null>(null);
  const importReviewRef = useRef<HTMLDivElement | null>(null);
  const profileImportFileRef = useRef<HTMLInputElement | null>(null);
  const draftReviewRef = useRef<HTMLElement | null>(null);
  const exportPanelRef = useRef<HTMLElement | null>(null);

  const selectedApplication = useMemo(
    () => applications.find((application) => application.id === selectedApplicationId),
    [applications, selectedApplicationId]
  );
  const approvedProfileFacts = useMemo(
    () => profileFacts.filter((fact) => fact.status === "Approved"),
    [profileFacts]
  );
  const allProfileFactsSelected = profileFacts.length > 0 && selectedProfileFactIds.length === profileFacts.length;
  const selectedImportedDraftFact = useMemo(
    () =>
      profileFacts.find(
        (fact) =>
          fact.id === selectedProfileFactId &&
          fact.status === "Draft" &&
          Boolean(importSessionIdFromProfileFact(fact))
      ),
    [profileFacts, selectedProfileFactId]
  );
  const selectedImportReviewQueue = useMemo(
    () =>
      selectedImportedDraftFact
        ? importReviewQueues.find((queue) => queue.importSessionId === importSessionIdFromProfileFact(selectedImportedDraftFact))
        : null,
    [importReviewQueues, selectedImportedDraftFact]
  );
  const filteredApplications = useMemo(() => {
    const search = applicationSearch.trim().toLowerCase();

    return applications.filter((application) => {
      const matchesStatus = applicationStatusFilter === "All" || application.status === applicationStatusFilter;
      const matchesReadiness =
        applicationReadinessFilter === "All" || application.auditReadiness === applicationReadinessFilter;
      const matchesSearch =
        !search ||
        application.companyName.toLowerCase().includes(search) ||
        application.roleTitle.toLowerCase().includes(search);

      return matchesStatus && matchesReadiness && matchesSearch;
    });
  }, [applications, applicationReadinessFilter, applicationSearch, applicationStatusFilter]);
  const jobSignals = useMemo(
    () => parseJobSignals(selectedApplication?.jobSignals),
    [selectedApplication?.jobSignals]
  );
  const evidenceMatches = useMemo(
    () => parseJsonArray<EvidenceMatch>(selectedApplication?.evidenceMatches),
    [selectedApplication?.evidenceMatches]
  );
  const unmatchedRequirements = useMemo(
    () => parseJsonArray<UnmatchedRequirement>(selectedApplication?.unmatchedRequirements),
    [selectedApplication?.unmatchedRequirements]
  );
  const savedApprovedEvidence = useMemo(
    () => parseJsonArray<EvidenceMatch>(selectedApplication?.approvedEvidence),
    [selectedApplication?.approvedEvidence]
  );
  const savedGapDecisions = useMemo(
    () => parseJsonArray<GapDecision>(selectedApplication?.gapDecisions),
    [selectedApplication?.gapDecisions]
  );
  const customFacts = useMemo(
    () => parseJsonArray<CustomFact>(selectedApplication?.customFacts),
    [selectedApplication?.customFacts]
  );
  const candidateFitBrief = useMemo(
    () => parseCandidateFitBrief(selectedApplication?.candidateFitBrief),
    [selectedApplication?.candidateFitBrief]
  );
  const applicationStrategy = useMemo(
    () => parseApplicationStrategy(selectedApplication?.applicationStrategy),
    [selectedApplication?.applicationStrategy]
  );
  const savedApprovedCustomFactEvidenceCount = useMemo(
    () => countApprovedCustomFactEvidence(savedGapDecisions, unmatchedRequirements, customFacts),
    [customFacts, savedGapDecisions, unmatchedRequirements]
  );
  const savedApprovedCustomFactEvidence = useMemo(
    () => approvedCustomFactEvidenceItems(savedGapDecisions, unmatchedRequirements, customFacts),
    [customFacts, savedGapDecisions, unmatchedRequirements]
  );
  const currentApprovedCustomFactEvidenceCount = useMemo(
    () => countApprovedCustomFactEvidence(gapDecisionsDraft, unmatchedRequirements, customFacts),
    [customFacts, gapDecisionsDraft, unmatchedRequirements]
  );
  const currentApprovedCustomFactEvidence = useMemo(
    () => approvedCustomFactEvidenceItems(gapDecisionsDraft, unmatchedRequirements, customFacts),
    [customFacts, gapDecisionsDraft, unmatchedRequirements]
  );
  const currentGapDecisionCount = useMemo(
    () => countCurrentGapDecisions(savedGapDecisions, unmatchedRequirements, customFacts),
    [customFacts, savedGapDecisions, unmatchedRequirements]
  );
  const claimAudit = useMemo(
    () => parseClaimAudit(selectedApplication?.generatedDraft?.claimAudit),
    [selectedApplication?.generatedDraft?.claimAudit]
  );
  const draftQualityCheck = useMemo(
    () => parseDraftQualityCheck(selectedApplication?.generatedDraft?.draftQualityCheck),
    [selectedApplication?.generatedDraft?.draftQualityCheck]
  );
  const isDraftQualityBlocked = draftQualityCheck.status === "NeedsRevision";
  const hasSavedJobPosting = Boolean(selectedApplication?.jobPostingText.trim());
  const hasSavedApprovedEvidence = savedApprovedEvidence.length + savedApprovedCustomFactEvidenceCount > 0;
  const hasGeneratedDraft = Boolean(selectedApplication?.generatedDraft);
  const recommendedEvidenceMatches = useMemo(
    () => evidenceMatches.filter(isRecommendedEvidence),
    [evidenceMatches]
  );
  const approvedRecommendedEvidenceCount = recommendedEvidenceMatches.filter((match) =>
    approvedEvidenceDraft.some((item) => item.id === match.id)
  ).length;
  const approvedEvidenceDraftSummaryCount = approvedEvidenceDraft.length + currentApprovedCustomFactEvidenceCount;
  const showCompactGapDecisionReview = !isEvidenceReviewEditing && savedGapDecisions.length > 0;
  const preparationDetailsOpen = Boolean(selectedApplication && !hasGeneratedDraft);
  const evidenceDetailsOpen = Boolean(selectedApplication && (!hasGeneratedDraft || !hasSavedApprovedEvidence));
  const strategyDetailsOpen = Boolean(selectedApplication && !hasGeneratedDraft);
  const preparationDetailsSummary = preparationSummary(
    selectedApplication?.preparationStatus,
    jobSignals,
    candidateFitBrief
  );
  const evidenceDetailsSummary = evidenceSummary(
    evidenceMatches.length,
    unmatchedRequirements.length,
    savedApprovedEvidence.length + savedApprovedCustomFactEvidenceCount,
    currentGapDecisionCount
  );
  const strategyDetailsSummary = strategySummary(applicationStrategy);
  const auditSummary = useMemo(() => summarizeClaimAudit(claimAudit), [claimAudit]);
  const hasUnsavedDraftEdits =
    Boolean(selectedApplication?.generatedDraft) &&
    (generatedDraftForm.coverLetterText !== selectedApplication?.generatedDraft?.coverLetterText ||
      generatedDraftForm.shortMotivationText !== selectedApplication?.generatedDraft?.shortMotivationText);
  const coverLetterExportState = useMemo(
    () =>
      getCoverLetterExportState(selectedApplication, {
        clipboardAvailable: isClipboardAvailable,
        currentCoverLetterText: generatedDraftForm.coverLetterText,
        hasUnsavedChanges: hasUnsavedDraftEdits
      }),
    [generatedDraftForm.coverLetterText, hasUnsavedDraftEdits, isClipboardAvailable, selectedApplication]
  );
  const auditExportNotice = useMemo(
    () => getAuditExportNotice(selectedApplication),
    [selectedApplication]
  );
  const effectiveAuditReadiness = hasUnsavedDraftEdits
    ? "Stale"
    : selectedApplication?.auditReadiness ?? auditReadinessForDraft(selectedApplication?.generatedDraft ?? null);
  const isRealProviderUnavailable = Boolean(aiStatus && !isFakeProvider(aiStatus) && !aiStatus.isAvailable);
  const canRefreshClaimAudit =
    Boolean(selectedApplication?.generatedDraft) &&
    effectiveAuditReadiness !== "Current" &&
    !isRealProviderUnavailable;
  const effectiveAuditExportNotice = isDraftQualityBlocked
    ? auditExportNotice
    : hasUnsavedDraftEdits
      ? {
          tone: "warning" as const,
          message: "Claim audit is stale because the draft has unsaved edits. Refresh claim audit to save and re-check the edited text."
        }
      : auditExportNotice;
  const profileReadiness = useMemo(
    () => getProfileReadiness(profile, approvedProfileFacts.length),
    [approvedProfileFacts.length, profile]
  );
  const jobAnalysisState = useMemo(
    () =>
      getJobAnalysisState({
        selectedApplicationId,
        hasSavedJobPosting
      }),
    [hasSavedJobPosting, selectedApplicationId]
  );
  const evidenceMatchingState = useMemo(
    () =>
      getEvidenceMatchingState({
        selectedApplicationId,
        hasJobSignals: jobSignals.signals.length > 0,
        approvedProfileFactCount: approvedProfileFacts.length
      }),
    [approvedProfileFacts.length, jobSignals.signals.length, selectedApplicationId]
  );
  const draftGenerationState = useMemo(
    () =>
      getDraftGenerationState({
        selectedApplicationId,
        hasSavedJobPosting,
        hasSavedApprovedEvidence,
        unmatchedRequirementCount: unmatchedRequirements.length,
        savedGapDecisionCount: currentGapDecisionCount,
        hasGeneratedDraft,
        aiStatus
      }),
    [aiStatus, currentGapDecisionCount, hasGeneratedDraft, hasSavedApprovedEvidence, hasSavedJobPosting, selectedApplicationId, unmatchedRequirements.length]
  );
  const guidedNextAction = useMemo(
    () =>
      getGuidedNextAction({
        selectedApplicationId,
        hasSavedJobPosting,
        preparationStatus: selectedApplication?.preparationStatus ?? "NotStarted",
        approvedProfileFactCount: approvedProfileFacts.length,
        savedApprovedEvidenceCount: savedApprovedEvidence.length + savedApprovedCustomFactEvidenceCount,
        unmatchedRequirementCount: unmatchedRequirements.length,
        savedGapDecisionCount: currentGapDecisionCount,
        hasGeneratedDraft,
        draftQualityStatus: draftQualityCheck.status,
        auditReadiness: effectiveAuditReadiness,
        hasUnsavedDraftEdits,
        canCopyOrExport: coverLetterExportState.canCopy || coverLetterExportState.canExport,
        aiStatus
      }),
    [
      aiStatus,
      approvedProfileFacts.length,
      coverLetterExportState.canCopy,
      coverLetterExportState.canExport,
      currentGapDecisionCount,
      draftQualityCheck.status,
      effectiveAuditReadiness,
      hasGeneratedDraft,
      hasSavedJobPosting,
      hasUnsavedDraftEdits,
      savedApprovedEvidence.length,
      savedApprovedCustomFactEvidenceCount,
      selectedApplication?.preparationStatus,
      selectedApplicationId,
      unmatchedRequirements.length
    ]
  );
  const workflowBusyReason = workflowBusy ? "Wait for the current workflow action to finish." : null;
  const exportBusyReason = exportBusy ? "Wait for the current export action to finish." : null;
  const canRunPreparation = Boolean(selectedApplicationId && hasSavedJobPosting && approvedProfileFacts.length > 0);

  useEffect(() => {
    void loadProfile();
    void loadProfileFacts();
    void loadAiStatus();
  }, []);

  useEffect(() => {
    setIsClipboardAvailable(Boolean(navigator.clipboard?.writeText));
  }, []);

  useEffect(() => {
    setApprovedEvidenceDraft(savedApprovedEvidence);
  }, [selectedApplicationId, savedApprovedEvidence]);

  useEffect(() => {
    setReviewedWeakMatchIds([]);
  }, [selectedApplicationId, selectedApplication?.evidenceMatches]);

  useEffect(() => {
    setGapDecisionsDraft(savedGapDecisions);
  }, [selectedApplicationId, savedGapDecisions]);

  useEffect(() => {
    setCustomFactDrafts({});
    setIsEvidenceReviewEditing(false);
    setExpandedCustomFactRequirementId(null);
  }, [selectedApplicationId]);

  useEffect(() => {
    setGeneratedDraftForm({
      coverLetterText: selectedApplication?.generatedDraft?.coverLetterText ?? "",
      shortMotivationText: selectedApplication?.generatedDraft?.shortMotivationText ?? ""
    });
    setExportFeedback(null);
  }, [selectedApplicationId, selectedApplication?.generatedDraft]);

  async function loadProfile() {
    try {
      const response = await apiGet<ProfileForm>("/api/profile");
      setProfile(toProfileForm(response));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function loadApplications() {
    try {
      const response = await apiGet<ApplicationSession[]>(
        `/api/applications?includeArchived=${includeArchivedApplications}`
      );
      setApplications(response.map(toApplicationSession));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  useEffect(() => {
    void loadApplications();
  }, [includeArchivedApplications]);

  useEffect(() => {
    void loadImportedDraftFactReviewQueues();
  }, [profileFacts]);

  useEffect(() => {
    if (!pendingImportReviewFocusId || !importReviewQueues.some((queue) => queue.importSessionId === pendingImportReviewFocusId)) {
      return;
    }

    importReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
    setPendingImportReviewFocusId(null);
  }, [importReviewQueues, pendingImportReviewFocusId]);

  async function loadProfileFacts() {
    try {
      const response = await apiGet<ProfileFact[]>("/api/profile/facts");
      const facts = response.map(toProfileFact);
      setProfileFacts(facts);
      setSelectedProfileFactIds((ids) => ids.filter((id) => facts.some((fact) => fact.id === id)));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function loadImportedDraftFactReviewQueues() {
    const importSessionIds = Array.from(new Set(profileFacts.map(importSessionIdFromProfileFact).filter((id): id is string => Boolean(id))));
    if (importSessionIds.length === 0) {
      setImportReviewQueues([]);
      return;
    }

    try {
      const queues = await Promise.all(
        importSessionIds.map((importSessionId) => apiGet<ImportedDraftFactReviewQueue>(`/api/profile/imports/${importSessionId}/draft-facts`))
      );
      setImportReviewQueues(queues.filter((queue) => queue.draftFactCount > 0));
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function loadAiStatus() {
    try {
      const response = await apiGet<AiProviderStatus>("/api/ai/status");
      setAiStatus(response);
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function saveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    try {
      const saved = await apiSend<ProfileForm>("/api/profile", "PUT", profile);
      setProfile(toProfileForm(saved));
      setNotice("Profile saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function saveApplication(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    try {
      const payload = toApplicationPayload(applicationForm);
      const path = selectedApplicationId
        ? `/api/applications/${selectedApplicationId}`
        : "/api/applications";
      const method = selectedApplicationId ? "PUT" : "POST";
      const saved = await apiSend<ApplicationSession>(path, method, payload);

      setApplicationForm(toApplicationForm(saved));
      setSelectedApplicationId(saved.id);
      await loadApplications();
      setNotice("Application saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function saveProfileFact(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    try {
      const path = selectedProfileFactId
        ? `/api/profile/facts/${selectedProfileFactId}`
        : "/api/profile/facts";
      const method = selectedProfileFactId ? "PUT" : "POST";
      const saved = await apiSend<ProfileFact>(path, method, profileFactForm);

      setProfileFactForm(toProfileFactForm(saved));
      setSelectedProfileFactId(saved.id);
      await loadProfileFacts();
      setNotice("Profile fact saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function deleteProfileFact() {
    if (!selectedProfileFactId) {
      return;
    }

    if (!window.confirm("Delete this profile fact? This permanently removes it from your evidence library.")) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      await apiDelete(`/api/profile/facts/${selectedProfileFactId}`);
      startNewProfileFact();
      setSelectedProfileFactIds((ids) => ids.filter((id) => id !== selectedProfileFactId));
      await loadProfileFacts();
      setNotice("Profile fact deleted.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function deleteSelectedProfileFacts() {
    if (selectedProfileFactIds.length === 0) {
      return;
    }

    const factCount = selectedProfileFactIds.length;
    const factLabel = factCount === 1 ? "profile fact" : "profile facts";
    if (!window.confirm(`Delete ${factCount} selected ${factLabel}? This permanently removes them from your evidence library.`)) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      await Promise.all(selectedProfileFactIds.map((id) => apiDelete(`/api/profile/facts/${id}`)));
      if (selectedProfileFactId && selectedProfileFactIds.includes(selectedProfileFactId)) {
        startNewProfileFact();
      }
      setSelectedProfileFactIds([]);
      await loadProfileFacts();
      setNotice(`${factCount} ${factLabel} deleted.`);
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function importProfilePdfCv(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profileImportFile) {
      setError(plainError("Validation blocker", "Choose a PDF CV before starting assisted import."));
      return;
    }

    setError(null);
    setNotice(null);
    setProfileImportFeedback(null);
    setProfileImportBusy(true);

    try {
      const form = new FormData();
      form.append("file", profileImportFile);
      const response = await apiSendForm<AssistedProfileImportResponse>("/api/profile/imports/pdf-cv", form);
      setImportReviewQueues((queues) => [
        response.reviewQueue,
        ...queues.filter((queue) => queue.importSessionId !== response.importSessionId)
      ]);
      setPendingImportReviewFocusId(response.importSessionId);
      setSelectedImportedDraftFactIds([]);
      if (response.profileFacts[0]) {
        setSelectedProfileFactId(response.profileFacts[0].id);
        setProfileFactForm(toProfileFactForm(response.profileFacts[0]));
        setSplitImportedDraftFacts(splitDraftTemplate(response.profileFacts[0]));
      }
      await loadProfileFacts();
      setProfileImportFile(null);
      if (profileImportFileRef.current) {
        profileImportFileRef.current.value = "";
      }
      setProfileImportFeedback({
        tone: "success",
        title: "Imported facts ready for review",
        message: `${response.importedFactCount} draft profile fact${response.importedFactCount === 1 ? "" : "s"} from ${response.fileName} are grouped below. Approve the strong ones to make them available for application evidence matching.`
      });
    } catch (apiError) {
      setError(formatError(apiError));
      setProfileImportFeedback({
        tone: "error",
        title: "Assisted import did not change your profile",
        message: "Review the diagnostics above, then retry the PDF import when the provider is ready."
      });
    } finally {
      setProfileImportBusy(false);
    }
  }

  async function reviewImportedDraftFact(
    fact: ProfileFact,
    decision: "approve" | "archive" | "reject",
    importSessionId = importSessionIdFromProfileFact(fact),
    profileFact?: ProfileFactForm
  ) {
    if (!importSessionId) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactDecisionResponse>(
        `/api/profile/imports/${importSessionId}/draft-facts/${fact.id}/decision`,
        "POST",
        {
          decision,
          profileFact
        }
      );
      setImportReviewQueues((queues) =>
        response.reviewQueue.draftFactCount > 0
          ? queues.map((queue) => (queue.importSessionId === response.reviewQueue.importSessionId ? response.reviewQueue : queue))
          : queues.filter((queue) => queue.importSessionId !== response.reviewQueue.importSessionId)
      );
      setProfileFactForm(toProfileFactForm(response.profileFact));
      setSelectedProfileFactId(response.profileFact.id);
      await loadProfileFacts();
      setNotice(
        decision === "approve"
          ? "Imported fact approved and available for application evidence matching."
          : `Imported fact ${importDecisionPastTense(decision)}.`
      );
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function bulkReviewImportedDraftFacts(queue: ImportedDraftFactReviewQueue, decision: "approve" | "archive") {
    const profileFactIds = selectedIdsForQueue(queue);
    if (profileFactIds.length === 0) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactBulkDecisionResponse>(
        `/api/profile/imports/${queue.importSessionId}/draft-facts/bulk-decision`,
        "POST",
        { decision, profileFactIds }
      );
      updateImportReviewQueue(response.reviewQueue);
      setSelectedImportedDraftFactIds((ids) => ids.filter((id) => !profileFactIds.includes(id)));
      await loadProfileFacts();
      setNotice(
        decision === "approve"
          ? `${profileFactIds.length} imported facts approved and available for application evidence matching.`
          : `${profileFactIds.length} imported facts ${importDecisionPastTense(decision)}.`
      );
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function mergeImportedDraftFacts(queue: ImportedDraftFactReviewQueue) {
    const profileFactIds = selectedIdsForQueue(queue);
    if (profileFactIds.length < 2) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactMergeResponse>(
        `/api/profile/imports/${queue.importSessionId}/draft-facts/merge`,
        "POST",
        { profileFactIds }
      );
      updateImportReviewQueue(response.reviewQueue);
      setSelectedImportedDraftFactIds((ids) => ids.filter((id) => !profileFactIds.includes(id)));
      setSelectedProfileFactId(response.profileFact.id);
      setProfileFactForm(toProfileFactForm(response.profileFact));
      await loadProfileFacts();
      setNotice("Imported facts merged.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function splitImportedDraftFact() {
    if (!selectedImportedDraftFact) {
      return;
    }

    const importSessionId = importSessionIdFromProfileFact(selectedImportedDraftFact);
    if (!importSessionId) {
      return;
    }

    let profileFacts: ProfileFactForm[];
    try {
      profileFacts = JSON.parse(splitImportedDraftFacts) as ProfileFactForm[];
    } catch {
      setError(plainError("Validation blocker", "Split facts must be a JSON array of profile fact drafts."));
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const response = await apiSend<ImportedDraftFactSplitResponse>(
        `/api/profile/imports/${importSessionId}/draft-facts/${selectedImportedDraftFact.id}/split`,
        "POST",
        { profileFacts }
      );
      updateImportReviewQueue(response.reviewQueue);
      setSelectedImportedDraftFactIds((ids) => ids.filter((id) => id !== selectedImportedDraftFact.id));
      if (response.profileFacts[0]) {
        setSelectedProfileFactId(response.profileFacts[0].id);
        setProfileFactForm(toProfileFactForm(response.profileFacts[0]));
      }
      setSplitImportedDraftFacts("");
      await loadProfileFacts();
      setNotice("Imported fact split.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  function updateImportReviewQueue(reviewQueue: ImportedDraftFactReviewQueue) {
    setImportReviewQueues((queues) =>
      reviewQueue.draftFactCount > 0
        ? queues.map((queue) => (queue.importSessionId === reviewQueue.importSessionId ? reviewQueue : queue))
        : queues.filter((queue) => queue.importSessionId !== reviewQueue.importSessionId)
    );
  }

  function toggleImportedDraftFactSelection(factId: string) {
    setSelectedImportedDraftFactIds((ids) =>
      ids.includes(factId) ? ids.filter((id) => id !== factId) : [...ids, factId]
    );
  }

  function toggleProfileFactSelection(factId: string) {
    setSelectedProfileFactIds((ids) =>
      ids.includes(factId) ? ids.filter((id) => id !== factId) : [...ids, factId]
    );
  }

  function toggleAllProfileFacts() {
    setSelectedProfileFactIds(allProfileFactsSelected ? [] : profileFacts.map((fact) => fact.id));
  }

  function selectedIdsForQueue(queue: ImportedDraftFactReviewQueue) {
    const queueIds = new Set(queue.groups.flatMap((group) => group.facts.map((item) => item.profileFact.id)));
    return selectedImportedDraftFactIds.filter((id) => queueIds.has(id));
  }

  async function deleteApplication() {
    if (!selectedApplicationId) {
      return;
    }

    if (!window.confirm("Delete this application session? This permanently removes its workflow state.")) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      await apiDelete(`/api/applications/${selectedApplicationId}`);
      setApplicationForm(emptyApplication);
      setSelectedApplicationId(null);
      await loadApplications();
      setNotice("Application deleted.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function markApplicationStatus(status: "Applied" | "Archived") {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before changing its final status."));
      return;
    }

    if (status === "Archived" && !window.confirm("Archive this application session? It will move out of the active history unless archived sessions are included.")) {
      return;
    }

    setError(null);
    setNotice(null);

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/status`,
        "PUT",
        { status }
      );
      replaceApplication(saved);
      await loadApplications();
      if (status === "Archived" && !includeArchivedApplications) {
        setSelectedApplicationId(null);
        setApplicationForm(emptyApplication);
      }
      setNotice(status === "Applied" ? "Application marked applied." : "Application archived.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function analyzeJob() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before running job analysis."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("analysis");

    try {
      const saved = await apiSend<ApplicationSession>(`/api/applications/${selectedApplicationId}/analyze-job`, "POST", null);
      replaceApplication(saved);
      setNotice("Job analysis updated.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function matchEvidence() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before matching evidence."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("matching");

    try {
      const saved = await apiSend<ApplicationSession>(`/api/applications/${selectedApplicationId}/match-evidence`, "POST", null);
      replaceApplication(saved);
      setNotice("Evidence matching updated.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function prepareApplication() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before preparing it."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("prepare");

    try {
      const result = await apiSend<PrepareApplicationResult>(
        getPrepareApplicationPath(selectedApplicationId),
        "POST",
        null
      );
      replaceApplication(result.application);
      setNotice(result.message);
    } catch (apiError) {
      setError(formatError(apiError));
      await refreshSelectedApplication(selectedApplicationId);
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function saveApprovedEvidence() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before reviewing evidence."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("review");

    try {
      await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/approved-evidence`,
        "PUT",
        { approvedEvidence: JSON.stringify(approvedEvidenceDraft) }
      );
      const reviewed = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/gap-decisions`,
        "PUT",
        { gapDecisions: JSON.stringify(gapDecisionsDraft) }
      );
      replaceApplication(reviewed);
      setIsEvidenceReviewEditing(false);
      setNotice("Evidence review saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function resetEvidenceReview() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before resetting evidence review."));
      return;
    }

    if (
      hasSavedApprovedEvidence &&
      !window.confirm("Reset saved evidence review? This clears approved evidence and gap decisions for this application.")
    ) {
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("review");
    setApprovedEvidenceDraft([]);
    setGapDecisionsDraft([]);
    setReviewedWeakMatchIds([]);
    setIsEvidenceReviewEditing(true);

    try {
      await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/approved-evidence`,
        "PUT",
        { approvedEvidence: "[]" }
      );
      const reviewed = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/gap-decisions`,
        "PUT",
        { gapDecisions: "[]" }
      );
      replaceApplication(reviewed);
      setNotice("Evidence review reset.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function createCustomFact(unmatchedRequirementId: string) {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before adding job-local facts."));
      return;
    }

    const draft = customFactDrafts[unmatchedRequirementId] ?? emptyCustomFactDraft();

    setError(null);
    setNotice(null);
    setWorkflowBusy(`custom-fact-${unmatchedRequirementId}`);

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/custom-facts`,
        "POST",
        {
          unmatchedRequirementId,
          title: draft.title,
          summary: draft.summary,
          technologies: splitLines(draft.technologies),
          allowedClaims: splitLines(draft.allowedClaims)
        }
      );
      replaceApplication(saved);
      setCustomFactDrafts((current) => ({ ...current, [unmatchedRequirementId]: emptyCustomFactDraft() }));
      setExpandedCustomFactRequirementId(null);
      setNotice("Job-local custom fact added for review.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function updateCustomFactStatus(customFactId: string, status: "Approved" | "Rejected") {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before reviewing job-local facts."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy(`custom-fact-status-${customFactId}`);

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/custom-facts/${customFactId}/status`,
        "PUT",
        { status }
      );
      replaceApplication(saved);
      setNotice(status === "Approved" ? "Job-local custom fact approved." : "Job-local custom fact rejected.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function generateDraft() {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before generating a draft."));
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("draft");

    try {
      const draft = await apiSend<GeneratedDraft>(`/api/applications/${selectedApplicationId}/generate-draft`, "POST", null);
      replaceGeneratedDraft(draft);
      setNotice(draft.auditUpdatedAt ? "Draft generated and claim audit updated." : "Draft generated. Claim audit needs retry before final review.");
      scrollDraftReviewSoon();
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function saveGeneratedDraft(): Promise<GeneratedDraft | null> {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before editing a draft."));
      return null;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("draft-edit");

    try {
      const draft = await apiSend<GeneratedDraft>(
        `/api/applications/${selectedApplicationId}/generated-draft`,
        "PUT",
        generatedDraftForm
      );
      replaceGeneratedDraft(draft);
      setNotice("Draft edits saved.");
      return draft;
    } catch (apiError) {
      setError(formatError(apiError));
      return null;
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function auditClaims(): Promise<GeneratedDraft | null> {
    if (!selectedApplicationId) {
      setError(plainError("Validation blocker", "Save the application before auditing a draft."));
      return null;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("audit");

    try {
      const draft = await apiSend<GeneratedDraft>(`/api/applications/${selectedApplicationId}/audit-claims`, "POST", null);
      replaceGeneratedDraft(draft);
      setNotice("Claim audit updated.");
      scrollDraftReviewSoon();
      return draft;
    } catch (apiError) {
      setError(formatError(apiError));
      return null;
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function refreshClaimAudit() {
    if (hasUnsavedDraftEdits) {
      const saved = await saveGeneratedDraft();
      if (!saved) {
        return;
      }
    }

    await auditClaims();
  }

  async function copyCoverLetter() {
    setError(null);
    setNotice(null);

    if (!coverLetterExportState.canCopy) {
      setExportFeedback({
        tone: "error",
        title: "Copy blocked",
        message: coverLetterExportState.reason ?? "Clipboard copy is not available in this browser."
      });
      return;
    }

    setExportFeedback(null);

    try {
      await navigator.clipboard.writeText(getCoverLetterText(selectedApplication, generatedDraftForm.coverLetterText));
      setExportFeedback({
        tone: "success",
        title: "Cover letter copied",
        message: "The current edited cover letter text is on the clipboard."
      });
    } catch (apiError) {
      setExportFeedback({
        tone: "error",
        title: "Clipboard copy failed",
        message: "Clipboard copy failed. You can still select and copy the cover letter manually.",
        details: technicalDetails(apiError)
      });
    }
  }

  async function downloadCoverLetter(format: "txt" | "docx") {
    setError(null);
    setNotice(null);

    if (!selectedApplicationId || !coverLetterExportState.canExport) {
      setExportFeedback({
        tone: "error",
        title: "Export blocked",
        message: coverLetterExportState.reason ?? "Select an application with a generated cover letter before exporting."
      });
      return;
    }

    setExportFeedback(null);
    setExportBusy(format);

    try {
      const response = await fetch(`${apiBaseUrl}/api/applications/${selectedApplicationId}/exports/cover-letter.${format}`);
      if (!response.ok) {
        throw await response.json();
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileNameFromContentDisposition(
        response.headers.get("content-disposition"),
        `cover-letter.${format}`
      );
      document.body.append(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      setExportFeedback({
        tone: "success",
        title: `${format.toUpperCase()} cover letter downloaded`,
        message: "The download used the current saved cover letter text without regenerating or re-running audit."
      });
    } catch (apiError) {
      const presentation = formatError(apiError);
      setExportFeedback({
        tone: "error",
        title: presentation.title,
        message: presentation.message,
        details: presentation.details
      });
    } finally {
      setExportBusy(null);
    }
  }

  async function runAiDiagnostics() {
    setError(null);
    setNotice(null);
    setAiDiagnosticsError(null);
    setAiDiagnosticsBusy(true);

    try {
      const diagnostics = await apiSend<AiDiagnostics>("/api/ai/diagnostics", "POST", null);
      setAiDiagnostics(diagnostics);
      setAiDiagnosticsLastRanAt(new Date().toISOString());
      setAiStatus({
        provider: diagnostics.provider,
        model: diagnostics.model,
        endpoint: diagnostics.endpoint,
        isAvailable: diagnostics.isAvailable,
        message: diagnostics.message
      });
      setNotice("AI diagnostics updated.");
    } catch (apiError) {
      setAiDiagnosticsLastRanAt(new Date().toISOString());
      setAiDiagnosticsError(formatError(apiError));
    } finally {
      setAiDiagnosticsBusy(false);
    }
  }

  async function openApplication(application: ApplicationSession) {
    setSelectedApplicationId(application.id);
    setApplicationForm(toApplicationForm(application));
    setView("applications");
    setError(null);
    setNotice(null);

    try {
      const detailed = await apiGet<ApplicationSession>(`/api/applications/${application.id}`);
      replaceApplication(detailed);
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function refreshSelectedApplication(applicationId: string) {
    try {
      const detailed = await apiGet<ApplicationSession>(`/api/applications/${applicationId}`);
      replaceApplication(detailed);
    } catch {
      // Keep the original preparation error visible.
    }
  }

  function runGuidedNextAction() {
    switch (guidedNextAction.kind) {
      case "prepare-application":
        void prepareApplication();
        return;
      case "review-evidence":
        evidenceReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      case "generate-draft":
        void generateDraft();
        return;
      case "revise-draft":
        draftReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      case "refresh-audit":
        void refreshClaimAudit();
        return;
      case "copy-export":
        exportPanelRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      case "ai-readiness":
        setView("settings");
        return;
      case "complete":
        draftReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        return;
      default:
        return;
    }
  }

  function scrollDraftReviewSoon() {
    window.setTimeout(() => draftReviewRef.current?.scrollIntoView({ behavior: "smooth", block: "start" }), 0);
  }

  function replaceApplication(application: ApplicationSession) {
    const normalized = toApplicationSession(application);
    setApplications((current) => current.map((item) => (item.id === normalized.id ? normalized : item)));
    setSelectedApplicationId(normalized.id);
    setApplicationForm(toApplicationForm(normalized));
  }

  function replaceGeneratedDraft(draft: GeneratedDraft) {
    setApplications((current) =>
      current.map((application) =>
        application.id === draft.jobApplicationId
          ? toApplicationSession({
              ...application,
              generatedDraft: draft,
              hasGeneratedDraft: true,
              auditReadiness: auditReadinessForDraft(draft),
              updatedAt: draft.updatedAt
            })
          : application
      )
    );
    setGeneratedDraftForm({
      coverLetterText: draft.coverLetterText,
      shortMotivationText: draft.shortMotivationText
    });
  }

  function approveMatch(match: EvidenceMatch) {
    if (!canApproveEvidenceMatch(match, reviewedWeakMatchIds.includes(match.id))) {
      return;
    }

    setApprovedEvidenceDraft((current) =>
      current.some((item) => item.id === match.id) ? current : [...current, match]
    );
  }

  function approveRecommendedEvidence() {
    setApprovedEvidenceDraft((current) => {
      const approvedIds = new Set(current.map((item) => item.id));
      const additions = recommendedEvidenceMatches.filter((match) => !approvedIds.has(match.id));
      return additions.length === 0 ? current : [...current, ...additions];
    });
  }

  function reviewWeakMatch(matchId: string) {
    setReviewedWeakMatchIds((current) => (current.includes(matchId) ? current : [...current, matchId]));
  }

  function removeApprovedEvidence(matchId: string) {
    setApprovedEvidenceDraft((current) => current.filter((item) => item.id !== matchId));
  }

  function decideGap(unmatchedRequirementId: string, decision: GapDecisionValue, customFactId?: string) {
    setGapDecisionsDraft((current) => [
      ...current.filter((item) => item.unmatchedRequirementId !== unmatchedRequirementId),
      { unmatchedRequirementId, decision, customFactId }
    ]);
    setExpandedCustomFactRequirementId((current) =>
      current === unmatchedRequirementId ? null : current
    );
  }

  function updateCustomFactDraft(unmatchedRequirementId: string, changes: Partial<CustomFactDraft>) {
    setCustomFactDrafts((current) => ({
      ...current,
      [unmatchedRequirementId]: {
        ...emptyCustomFactDraft(),
        ...current[unmatchedRequirementId],
        ...changes
      }
    }));
  }

  function startNewApplication() {
    setSelectedApplicationId(null);
    setApplicationForm(emptyApplication);
    setView("applications");
    setError(null);
    setNotice(null);
  }

  function openProfileFact(fact: ProfileFact) {
    setSelectedProfileFactId(fact.id);
    setProfileFactForm(toProfileFactForm(fact));
    setSplitImportedDraftFacts(importSessionIdFromProfileFact(fact) ? splitDraftTemplate(fact) : "");
    setError(null);
    setNotice(null);
  }

  function startNewProfileFact() {
    setSelectedProfileFactId(null);
    setProfileFactForm(emptyProfileFact);
    setSplitImportedDraftFacts("");
    setError(null);
    setNotice(null);
  }

  return (
    <AppShell
      view={view}
      onViewChange={setView}
      title={pageTitle(view)}
      aiStatus={aiStatus}
      error={error}
      notice={notice}
    >
        {view === "home" && (
          <HomeView
            profileReadiness={profileReadiness}
            applications={applications}
            approvedProfileFactCount={approvedProfileFacts.length}
            aiStatusContent={
              aiStatus ? (
                <ProviderReadinessSummary
                  status={aiStatus}
                  diagnostics={aiDiagnostics}
                  diagnosticsLastRanAt={aiDiagnosticsLastRanAt}
                  compact
                />
              ) : (
                <p>Loading AI provider status.</p>
              )
            }
            onReviewProfile={() => setView("profile")}
            onNewApplication={startNewApplication}
            onOpenApplication={(application) => void openApplication(application)}
            getApplicationNextActionLabel={applicationNextActionLabel}
          />
        )}

        {view === "profile" && (
          <div className="profile-layout">
            <ProfileFormPanel
              profile={profile}
              profileReadiness={profileReadiness}
              onChange={setProfile}
              onSubmit={saveProfile}
            />

            <section className="profile-facts-panel">
              <div className="section-heading">
                <h3>Profile facts</h3>
                <p>{profileReadiness.evidenceMessage}</p>
              </div>
              <ProfileImportPanel
                isFakeAiProvider={Boolean(aiStatus && isFakeProvider(aiStatus))}
                busy={profileImportBusy}
                feedback={profileImportFeedback}
                fileInputRef={profileImportFileRef}
                onFileChange={setProfileImportFile}
                onSubmit={importProfilePdfCv}
              />
              {!profileReadiness.hasApprovedEvidence && (
                <p className="workflow-note warning">Approved evidence is required before evidence matching and draft generation. Draft or archived facts will not be used as proof.</p>
              )}
              <ImportReviewQueues
                queues={importReviewQueues}
                selectedImportedDraftFactIds={selectedImportedDraftFactIds}
                containerRef={importReviewRef}
                selectedIdsForQueue={selectedIdsForQueue}
                duplicateScopeLabel={duplicateScopeLabel}
                onToggleSelection={toggleImportedDraftFactSelection}
                onOpenFact={openProfileFact}
                onMerge={(queue) => void mergeImportedDraftFacts(queue)}
                onBulkApprove={(queue) => void bulkReviewImportedDraftFacts(queue, "approve")}
                onBulkArchive={(queue) => void bulkReviewImportedDraftFacts(queue, "archive")}
              />
              <ProfileFactsPanel
                profileFacts={profileFacts}
                selectedProfileFactId={selectedProfileFactId}
                selectedProfileFactIds={selectedProfileFactIds}
                allProfileFactsSelected={allProfileFactsSelected}
                statusTone={statusTone}
                profileFactStatusLabel={profileFactStatusLabel}
                onToggleAll={toggleAllProfileFacts}
                onToggleFact={toggleProfileFactSelection}
                onOpenFact={openProfileFact}
                onDeleteSelected={() => void deleteSelectedProfileFacts()}
              />
              <ProfileFactEditor
                profileFactForm={profileFactForm}
                profileFactStatuses={profileFactStatuses}
                selectedProfileFactId={selectedProfileFactId}
                selectedImportedDraftFact={selectedImportedDraftFact}
                selectedImportReviewQueueExists={Boolean(selectedImportReviewQueue)}
                splitImportedDraftFacts={splitImportedDraftFacts}
                onFormChange={setProfileFactForm}
                onSplitChange={setSplitImportedDraftFacts}
                onSave={saveProfileFact}
                onApproveImport={() => {
                  if (selectedImportedDraftFact) {
                    void reviewImportedDraftFact(
                      selectedImportedDraftFact,
                      "approve",
                      undefined,
                      profileFactFormChanged(selectedImportedDraftFact, profileFactForm) ? profileFactForm : undefined
                    );
                  }
                }}
                onArchiveImport={() => {
                  if (selectedImportedDraftFact) {
                    void reviewImportedDraftFact(selectedImportedDraftFact, "archive");
                  }
                }}
                onSplitImport={() => void splitImportedDraftFact()}
                onRejectImport={() => {
                  if (selectedImportedDraftFact) {
                    void reviewImportedDraftFact(selectedImportedDraftFact, "reject");
                  }
                }}
                onClear={startNewProfileFact}
                onDelete={() => void deleteProfileFact()}
              />
            </section>
          </div>
        )}

        {view === "applications" && (
          <div className="applications-layout">
            <ApplicationListPanel
              applications={applications}
              filteredApplications={filteredApplications}
              selectedApplicationId={selectedApplicationId}
              approvedProfileFactCount={approvedProfileFacts.length}
              includeArchivedApplications={includeArchivedApplications}
              applicationSearch={applicationSearch}
              applicationStatusFilter={applicationStatusFilter}
              applicationReadinessFilter={applicationReadinessFilter}
              applicationStatuses={applicationStatuses}
              auditReadinessOptions={auditReadinessOptions}
              onSearchChange={setApplicationSearch}
              onStatusFilterChange={setApplicationStatusFilter}
              onReadinessFilterChange={setApplicationReadinessFilter}
              onIncludeArchivedChange={setIncludeArchivedApplications}
              onNewApplication={startNewApplication}
              onOpenApplication={(application) => void openApplication(application)}
              applicationStatusClass={applicationStatusClass}
              applicationHistoryTone={applicationHistoryTone}
              applicationHistoryLabel={applicationHistoryLabel}
              applicationStatusLabel={applicationStatusLabel}
              applicationNextActionLabel={applicationNextActionLabel}
              readinessLabel={readinessLabel}
              applicationHistoryEmptyMessage={applicationHistoryEmptyMessage}
              formatDate={formatDate}
            />
            <ApplicationEditor
              selectedApplication={selectedApplication}
              selectedApplicationId={selectedApplicationId}
              applicationForm={applicationForm}
              applicationStatuses={applicationStatuses}
              workflowBusy={workflowBusy}
              workflowBusyReason={workflowBusyReason}
              onApplicationFormChange={setApplicationForm}
              onSaveApplication={saveApplication}
              onStartNewApplication={startNewApplication}
              onDeleteApplication={() => void deleteApplication()}
              onMarkApplied={() => void markApplicationStatus("Applied")}
              onArchive={() => void markApplicationStatus("Archived")}
              disabledTitle={disabledTitle}
            >
              <ApplicationWorkflowPanel
                selectedApplication={selectedApplication}
                selectedApplicationId={selectedApplicationId}
                hasGeneratedDraft={hasGeneratedDraft}
                hasSavedApprovedEvidence={hasSavedApprovedEvidence}
                savedApprovedEvidence={savedApprovedEvidence}
                savedApprovedCustomFactEvidenceCount={savedApprovedCustomFactEvidenceCount}
                guidedNextAction={guidedNextAction}
                jobAnalysisState={jobAnalysisState}
                evidenceMatchingState={evidenceMatchingState}
                draftGenerationState={draftGenerationState}
                coverLetterExportState={coverLetterExportState}
                jobSignals={jobSignals}
                evidenceMatches={evidenceMatches}
                unmatchedRequirements={unmatchedRequirements}
                savedGapDecisions={savedGapDecisions}
                gapDecisionsDraft={gapDecisionsDraft}
                customFacts={customFacts}
                customFactDrafts={customFactDrafts}
                expandedCustomFactRequirementId={expandedCustomFactRequirementId}
                savedApprovedCustomFactEvidence={savedApprovedCustomFactEvidence}
                currentApprovedCustomFactEvidence={currentApprovedCustomFactEvidence}
                candidateFitBrief={candidateFitBrief}
                applicationStrategy={applicationStrategy}
                claimAudit={claimAudit}
                draftQualityCheck={draftQualityCheck}
                approvedEvidenceDraft={approvedEvidenceDraft}
                approvedEvidenceDraftSummaryCount={approvedEvidenceDraftSummaryCount}
                approvedRecommendedEvidenceCount={approvedRecommendedEvidenceCount}
                recommendedEvidenceMatches={recommendedEvidenceMatches}
                reviewedWeakMatchIds={reviewedWeakMatchIds}
                currentGapDecisionCount={currentGapDecisionCount}
                auditSummary={auditSummary}
                effectiveAuditReadiness={effectiveAuditReadiness}
                effectiveAuditExportNotice={effectiveAuditExportNotice}
                isDraftQualityBlocked={isDraftQualityBlocked}
                hasUnsavedDraftEdits={hasUnsavedDraftEdits}
                isRealProviderUnavailable={isRealProviderUnavailable}
                canRefreshClaimAudit={canRefreshClaimAudit}
                generatedDraftForm={generatedDraftForm}
                preparationDetailsOpen={preparationDetailsOpen}
                evidenceDetailsOpen={evidenceDetailsOpen}
                strategyDetailsOpen={strategyDetailsOpen}
                preparationDetailsSummary={preparationDetailsSummary}
                evidenceDetailsSummary={evidenceDetailsSummary}
                strategyDetailsSummary={strategyDetailsSummary}
                showCompactGapDecisionReview={showCompactGapDecisionReview}
                canRunPreparation={canRunPreparation}
                workflowBusy={workflowBusy}
                workflowBusyReason={workflowBusyReason}
                exportBusy={exportBusy}
                exportBusyReason={exportBusyReason}
                exportFeedback={exportFeedback}
                aiStatus={aiStatus}
                aiDiagnostics={aiDiagnostics}
                aiDiagnosticsLastRanAt={aiDiagnosticsLastRanAt}
                evidenceReviewRef={evidenceReviewRef}
                draftReviewRef={draftReviewRef}
                exportPanelRef={exportPanelRef}
                onRunGuidedNextAction={runGuidedNextAction}
                onPrepareApplication={prepareApplication}
                onAnalyzeJob={analyzeJob}
                onMatchEvidence={matchEvidence}
                onGenerateDraft={generateDraft}
                onApproveMatch={approveMatch}
                onApproveRecommendedEvidence={approveRecommendedEvidence}
                onReviewWeakMatch={reviewWeakMatch}
                onRemoveApprovedEvidence={removeApprovedEvidence}
                onSetEvidenceReviewEditing={setIsEvidenceReviewEditing}
                onDecideGap={decideGap}
                onToggleCustomFactEditor={(requirementId) =>
                  setExpandedCustomFactRequirementId((current) => (current === requirementId ? null : requirementId))
                }
                onUpdateCustomFactDraft={updateCustomFactDraft}
                onCreateCustomFact={createCustomFact}
                onUpdateCustomFactStatus={updateCustomFactStatus}
                onResetEvidenceReview={resetEvidenceReview}
                onSaveApprovedEvidence={saveApprovedEvidence}
                onDraftFormChange={setGeneratedDraftForm}
                onSaveGeneratedDraft={() => void saveGeneratedDraft()}
                onRefreshClaimAudit={() => void refreshClaimAudit()}
                onCopyCoverLetter={() => void copyCoverLetter()}
                onDownloadCoverLetter={(format) => void downloadCoverLetter(format)}
                onClearExportFeedback={() => setExportFeedback(null)}
                approvedEvidenceCountLabel={approvedEvidenceCountLabel}
                auditReadinessLabel={auditReadinessLabel}
                auditReadinessTone={auditReadinessTone}
                claimAuditMessage={claimAuditMessage}
                customFactStatusClass={customFactStatusClass}
                customFactStatusLabel={customFactStatusLabel}
                customFactStatusTone={customFactStatusTone}
                disabledTitle={disabledTitle}
                emptyCustomFactDraft={emptyCustomFactDraft}
                formatDate={formatDate}
                gapDecisionClass={gapDecisionClass}
                gapDecisionForRequirement={gapDecisionForRequirement}
                gapDecisionLabel={gapDecisionLabel}
                gapDecisionSummary={gapDecisionSummary}
                gapDecisionTone={gapDecisionTone}
                guidedActionButtonLabel={guidedActionButtonLabel}
                guidedStepLabel={guidedStepLabel}
                preparationStatusLabel={preparationStatusLabel}
                preparationStatusTone={preparationStatusTone}
                customFactsForRequirement={customFactsForRequirement}
              />
            </ApplicationEditor>
          </div>
        )}

        {view === "settings" && (
          <SettingsView
            aiStatus={aiStatus}
            aiDiagnostics={aiDiagnostics}
            aiDiagnosticsError={aiDiagnosticsError}
            aiDiagnosticsBusy={aiDiagnosticsBusy}
            aiDiagnosticsLastRanAt={aiDiagnosticsLastRanAt}
            onRunAiDiagnostics={runAiDiagnostics}
            formatDateTime={formatDateTime}
          />
        )}
    </AppShell>
  );
}

function toProfileForm(profile: ProfileForm): ProfileForm {
  return {
    ...emptyProfile,
    ...profile,
    phone: profile.phone ?? "",
    location: profile.location ?? "",
    linkedInUrl: profile.linkedInUrl ?? "",
    gitHubUrl: profile.gitHubUrl ?? "",
    portfolioUrl: profile.portfolioUrl ?? "",
    danishTone: profile.danishTone ?? "",
    englishTone: profile.englishTone ?? ""
  };
}

function toProfileFactForm(fact: ProfileFactForm): ProfileFactForm {
  return {
    ...emptyProfileFact,
    ...fact,
    factItems: fact.factItems || "[]",
    technologies: fact.technologies || "[]",
    allowedClaims: fact.allowedClaims || "[]",
    forbiddenClaims: fact.forbiddenClaims || "[]"
  };
}

function toProfileFact(fact: ProfileFact): ProfileFact {
  return {
    ...toProfileFactForm(fact),
    id: fact.id,
    createdAt: fact.createdAt,
    updatedAt: fact.updatedAt,
    sourceDocumentIds: fact.sourceDocumentIds ?? "[]",
    originalImportedSnapshot: fact.originalImportedSnapshot ?? null,
    manuallyEdited: Boolean(fact.manuallyEdited)
  };
}

function importSessionIdFromProfileFact(fact: ProfileFact): string | null {
  if (!fact.originalImportedSnapshot || fact.status !== "Draft") {
    return null;
  }

  try {
    const snapshot = JSON.parse(fact.originalImportedSnapshot) as { importSessionId?: string };
    return snapshot.importSessionId ?? null;
  } catch {
    return null;
  }
}

function duplicateScopeLabel(scope: string): string {
  if (scope === "ImportBatch") {
    return "Same import";
  }

  if (scope === "ExistingProfileFact") {
    return "Existing fact";
  }

  return "Overlap";
}

function importDecisionPastTense(decision: "approve" | "archive" | "reject"): string {
  switch (decision) {
    case "approve":
      return "approved";
    case "archive":
      return "archived";
    case "reject":
      return "rejected";
  }
}

function profileFactFormChanged(fact: ProfileFact, form: ProfileFactForm): boolean {
  return (
    fact.type !== form.type ||
    fact.title !== form.title ||
    fact.summary !== form.summary ||
    fact.status !== form.status ||
    fact.factItems !== form.factItems ||
    fact.technologies !== form.technologies ||
    fact.allowedClaims !== form.allowedClaims ||
    fact.forbiddenClaims !== form.forbiddenClaims
  );
}

function splitDraftTemplate(fact: ProfileFact): string {
  const first = toProfileFactForm(fact);
  const second = toProfileFactForm(fact);
  first.title = `${fact.title} - part 1`;
  second.title = `${fact.title} - part 2`;
  first.summary = "";
  second.summary = "";

  return JSON.stringify([first, second], null, 2);
}

function toApplicationForm(application: ApplicationForm) {
  return {
    ...application,
    applicationUrl: application.applicationUrl ?? "",
    deadline: application.deadline ?? "",
    jobPostingText: application.jobPostingText ?? "",
    detectedLanguage: application.detectedLanguage ?? "",
    selectedLanguage: application.selectedLanguage ?? ""
  };
}

function toApplicationSession(application: ApplicationSession): ApplicationSession {
  return {
    ...toApplicationForm(application),
    id: application.id,
    jobSignals: application.jobSignals || "{}",
    evidenceMatches: application.evidenceMatches || "[]",
    unmatchedRequirements: application.unmatchedRequirements || "[]",
    candidateFitBrief: application.candidateFitBrief || "{}",
    approvedEvidence: application.approvedEvidence || "[]",
    gapDecisions: application.gapDecisions || "[]",
    customFacts: application.customFacts || "[]",
    applicationStrategy: application.applicationStrategy || "{}",
    lastPreparedAt: application.lastPreparedAt ?? null,
    preparationStatus: application.preparationStatus ?? "NotStarted",
    generatedDraft: application.generatedDraft ?? null,
    hasGeneratedDraft: application.hasGeneratedDraft ?? Boolean(application.generatedDraft),
    auditReadiness: application.auditReadiness ?? auditReadinessForDraft(application.generatedDraft),
    createdAt: application.createdAt,
    updatedAt: application.updatedAt
  };
}

function toApplicationPayload(application: ApplicationForm) {
  return {
    ...application,
    applicationUrl: application.applicationUrl || null,
    deadline: application.deadline || null,
    detectedLanguage: application.detectedLanguage || null,
    selectedLanguage: application.selectedLanguage || null
  };
}

function fileNameFromContentDisposition(header: string | null, fallback: string): string {
  if (!header) {
    return fallback;
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const fileNameMatch = /filename="?([^";]+)"?/i.exec(header);
  return fileNameMatch?.[1] ?? fallback;
}

function claimAuditMessage(hasGeneratedDraft: boolean): string {
  return hasGeneratedDraft
    ? "Run claim audit after reviewing the generated text."
    : "Generate a draft before running claim audit.";
}

function auditReadinessForDraft(draft: GeneratedDraft | null): string {
  if (!draft) {
    return "NotApplicable";
  }

  if (draft.isClaimAuditStale) {
    return "Stale";
  }

  if (!draft.auditUpdatedAt || draft.claimAudit === "{}") {
    return "Missing";
  }

  return "Current";
}

function readinessLabel(readiness: string): string {
  return readiness === "NotApplicable" ? "not applicable" : readiness.toLowerCase();
}

function approvedEvidenceCountLabel(count: number): string {
  return count === 1 ? "1 approved evidence item" : `${count} approved evidence items`;
}

function preparationSummary(
  preparationStatus: string | undefined,
  jobSignals: JobSignalsDocument,
  candidateFitBrief: CandidateFitBrief | null
): string {
  const signalSummary = countLabel(jobSignals.signals.length, "job signal");
  const fitBriefSummary = hasCandidateFitBriefContent(candidateFitBrief) ? "fit brief ready" : "no fit brief";

  return `${preparationStatusLabel(preparationStatus ?? "NotStarted")}; ${signalSummary}; ${fitBriefSummary}`;
}

function evidenceSummary(
  matchCount: number,
  unmatchedRequirementCount: number,
  approvedEvidenceCount: number,
  gapDecisionCount: number
): string {
  return [
    countLabel(approvedEvidenceCount, "approved item"),
    countLabel(matchCount, "match"),
    countLabel(unmatchedRequirementCount, "gap"),
    countLabel(gapDecisionCount, "decision")
  ].join("; ");
}

function strategySummary(strategy: ApplicationStrategy | null): string {
  if (!hasApplicationStrategyContent(strategy)) {
    return "No strategy yet";
  }

  return [
    countLabel(strategy.primaryAngles.length, "primary angle"),
    countLabel(strategy.claimsToAvoid.length, "claim to avoid", "claims to avoid"),
    countLabel(strategy.draftOutline.length, "outline item")
  ].join("; ");
}

function countLabel(count: number, singular: string, plural = `${singular}s`): string {
  return `${count} ${count === 1 ? singular : plural}`;
}

function auditReadinessLabel(readiness: string): string {
  return `Audit ${readinessLabel(readiness)}`;
}

function auditReadinessTone(readiness: string): string {
  switch (readiness) {
    case "Current":
      return "approved";
    case "Stale":
    case "Missing":
      return "pending";
    default:
      return "neutral";
  }
}

function applicationHistoryLabel(status: string): string {
  if (status === "Applied") {
    return "Applied";
  }

  if (status === "Archived") {
    return "Archived";
  }

  return "Active";
}

function applicationHistoryTone(status: string): string {
  if (status === "Applied") {
    return "applied";
  }

  if (status === "Archived") {
    return "archived";
  }

  return "active-work";
}

function applicationStatusClass(status: string): string {
  if (status === "Applied") {
    return "applied";
  }

  if (status === "Archived") {
    return "archived";
  }

  return "active-work";
}

function applicationStatusLabel(status: string): string {
  switch (status) {
    case "PostingCaptured":
      return "Posting captured";
    case "ReadyForReview":
      return "Ready for review";
    default:
      return status;
  }
}

function applicationNextActionLabel(application: ApplicationSession, approvedProfileFactCount: number): string {
  const unmatchedRequirements = parseJsonArray<UnmatchedRequirement>(application.unmatchedRequirements);
  const gapDecisions = parseJsonArray<GapDecision>(application.gapDecisions);
  const customFacts = parseJsonArray<CustomFact>(application.customFacts);

  return getGuidedNextAction({
    selectedApplicationId: application.id,
    hasSavedJobPosting: Boolean(application.jobPostingText.trim()),
    preparationStatus: application.preparationStatus,
    approvedProfileFactCount,
    savedApprovedEvidenceCount:
      parseJsonArray<EvidenceMatch>(application.approvedEvidence).length +
      countApprovedCustomFactEvidence(gapDecisions, unmatchedRequirements, customFacts),
    unmatchedRequirementCount: unmatchedRequirements.length,
    savedGapDecisionCount: countCurrentGapDecisions(gapDecisions, unmatchedRequirements, customFacts),
    hasGeneratedDraft: application.hasGeneratedDraft,
    draftQualityStatus: parseDraftQualityCheck(application.generatedDraft?.draftQualityCheck).status,
    auditReadiness: application.auditReadiness ?? auditReadinessForDraft(application.generatedDraft),
    canCopyOrExport: Boolean(application.generatedDraft?.coverLetterText.trim())
  }).title;
}

function countCurrentGapDecisions(
  decisions: GapDecision[],
  requirements: UnmatchedRequirement[],
  customFacts: CustomFact[] = []
): number {
  const requirementIds = new Set(requirements.map((requirement) => requirement.id));
  const approvedCustomFactsById = new Map(
    customFacts
      .filter((fact) => fact.status === "Approved")
      .map((fact) => [fact.id, fact])
  );
  const decidedRequirementIds = new Set(
    decisions
      .filter((decision) => {
        if (!requirementIds.has(decision.unmatchedRequirementId)) {
          return false;
        }

        if (decision.decision !== "CoveredByCustomFact") {
          return true;
        }

        const customFact = decision.customFactId ? approvedCustomFactsById.get(decision.customFactId) : null;
        return customFact?.unmatchedRequirementId === decision.unmatchedRequirementId;
      })
      .map((decision) => decision.unmatchedRequirementId)
  );

  return decidedRequirementIds.size;
}

function countApprovedCustomFactEvidence(
  decisions: GapDecision[],
  requirements: UnmatchedRequirement[],
  customFacts: CustomFact[]
): number {
  const requirementIds = new Set(requirements.map((requirement) => requirement.id));
  const approvedCustomFactsById = new Map(
    customFacts
      .filter((fact) => fact.status === "Approved")
      .map((fact) => [fact.id, fact])
  );

  return new Set(
    decisions
      .filter((decision) => {
        if (decision.decision !== "CoveredByCustomFact" || !requirementIds.has(decision.unmatchedRequirementId)) {
          return false;
        }

        const customFact = decision.customFactId ? approvedCustomFactsById.get(decision.customFactId) : null;
        return customFact?.unmatchedRequirementId === decision.unmatchedRequirementId;
      })
      .map((decision) => decision.unmatchedRequirementId)
  ).size;
}

function approvedCustomFactEvidenceItems(
  decisions: GapDecision[],
  requirements: UnmatchedRequirement[],
  customFacts: CustomFact[]
): Array<{ requirement: UnmatchedRequirement; fact: CustomFact }> {
  const requirementsById = new Map(requirements.map((requirement) => [requirement.id, requirement]));
  const approvedCustomFactsById = new Map(
    customFacts
      .filter((fact) => fact.status === "Approved")
      .map((fact) => [fact.id, fact])
  );

  return decisions
    .filter((decision) => decision.decision === "CoveredByCustomFact" && decision.customFactId)
    .map((decision) => {
      const requirement = requirementsById.get(decision.unmatchedRequirementId);
      const fact = approvedCustomFactsById.get(decision.customFactId ?? "");

      return requirement && fact?.unmatchedRequirementId === requirement.id ? { requirement, fact } : null;
    })
    .filter((item): item is { requirement: UnmatchedRequirement; fact: CustomFact } => item !== null);
}

function customFactsForRequirement(facts: CustomFact[], unmatchedRequirementId: string): CustomFact[] {
  return facts.filter((fact) => fact.unmatchedRequirementId === unmatchedRequirementId);
}

function gapDecisionForRequirement(decisions: GapDecision[], unmatchedRequirementId: string): GapDecision | undefined {
  return decisions.find((decision) => decision.unmatchedRequirementId === unmatchedRequirementId);
}

function gapDecisionLabel(decision: GapDecisionValue): string {
  switch (decision) {
    case "Ignore":
      return "Ignored gap";
    case "MentionAsLearningInterest":
      return "Learning-interest gap";
    case "CoveredByCustomFact":
      return "Covered by custom fact";
    default:
      return decision;
  }
}

function gapDecisionTone(decision: GapDecision): string {
  if (decision.decision === "Ignore") {
    return "neutral";
  }

  return decision.decision === "CoveredByCustomFact" ? "approved" : "pending";
}

function gapDecisionClass(decision: GapDecision): string {
  switch (decision.decision) {
    case "Ignore":
      return "ignored";
    case "CoveredByCustomFact":
      return "covered";
    default:
      return "learning";
  }
}

function gapDecisionSummary(decision: GapDecision, customFacts: CustomFact[]): string {
  switch (decision.decision) {
    case "Ignore":
      return "Will not be called out in the review guidance.";
    case "CoveredByCustomFact": {
      const customFact = customFacts.find((fact) => fact.id === decision.customFactId);
      return customFact ? `Supported by approved job-local fact: ${customFact.title}.` : "Requires an approved job-local custom fact.";
    }
    default:
      return "May be framed cautiously as interest, not proof.";
  }
}

function emptyCustomFactDraft(): CustomFactDraft {
  return {
    title: "",
    summary: "",
    technologies: "",
    allowedClaims: ""
  };
}

function splitLines(value: string): string[] {
  return value
    .split(/\r?\n|,/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function guidedActionButtonLabel(label: string, kind: string, workflowBusy: string | null): string {
  if (workflowBusy === "prepare" && kind === "prepare-application") {
    return "Preparing...";
  }

  if (workflowBusy === "review" && kind === "review-evidence") {
    return "Saving...";
  }

  if ((workflowBusy === "draft-edit" || workflowBusy === "audit") && kind === "refresh-audit") {
    return workflowBusy === "draft-edit" ? "Saving..." : "Auditing...";
  }

  if (workflowBusy === "draft" && kind === "generate-draft") {
    return "Generating...";
  }

  return label;
}

function guidedStepLabel(kind: string): string {
  switch (kind) {
    case "save-posting":
      return "Setup";
    case "prepare-application":
    case "ai-readiness":
      return "Step 1";
    case "review-evidence":
      return "Step 2";
    case "generate-draft":
    case "revise-draft":
    case "refresh-audit":
      return "Step 3";
    case "copy-export":
    case "complete":
      return "Final";
    default:
      return "Next action";
  }
}

function preparationStatusLabel(status: string): string {
  switch (status) {
    case "PreparedForEvidenceReview":
      return "Prepared for evidence review";
    case "Preparing":
      return "Preparing";
    case "FailedProviderUnavailable":
      return "Preparation blocked";
    case "FailedInvalidProviderOutput":
      return "Preparation output blocked";
    case "PartiallyPreparedAnalysisOnly":
      return "Analysis prepared";
    default:
      return "Preparation not started";
  }
}

function preparationStatusTone(status: string): string {
  switch (status) {
    case "PreparedForEvidenceReview":
      return "approved";
    case "FailedProviderUnavailable":
    case "FailedInvalidProviderOutput":
      return "rejected";
    case "Preparing":
    case "PartiallyPreparedAnalysisOnly":
      return "pending";
    default:
      return "neutral";
  }
}

function profileFactStatusLabel(status: string): string {
  switch (status) {
    case "Draft":
      return "Draft evidence";
    case "Approved":
      return "Approved evidence";
    case "Archived":
      return "Archived evidence";
    case "Rejected":
      return "Rejected evidence";
    default:
      return status;
  }
}

function customFactStatusLabel(status: string): string {
  switch (status) {
    case "PendingConfirmation":
      return "Pending confirmation";
    case "Approved":
      return "Approved custom fact";
    case "Rejected":
      return "Rejected custom fact";
    default:
      return status;
  }
}

function customFactStatusClass(status: string): string {
  switch (status) {
    case "PendingConfirmation":
      return "pending";
    case "Approved":
      return "approved";
    case "Rejected":
      return "rejected";
    default:
      return "neutral";
  }
}

function customFactStatusTone(status: string): string {
  switch (status) {
    case "PendingConfirmation":
      return "pending";
    case "Approved":
      return "approved";
    case "Rejected":
      return "rejected";
    default:
      return "neutral";
  }
}

function statusTone(status: string): string {
  switch (status) {
    case "Draft":
      return "draft";
    case "Approved":
      return "approved";
    case "Archived":
      return "archived";
    case "Rejected":
      return "rejected";
    default:
      return "neutral";
  }
}

function applicationHistoryEmptyMessage(includeArchived: boolean): string {
  return includeArchived
    ? "No application sessions yet."
    : "No active application sessions yet. Create one here, or include archived sessions to review older work.";
}

function summarizeClaimAudit(audit: ClaimAudit) {
  return audit.claims.reduce(
    (summary, claim) => {
      if (claim.status === "Supported") {
        return { ...summary, supported: summary.supported + 1 };
      }

      if (claim.status === "Unsupported") {
        return { ...summary, unsupported: summary.unsupported + 1 };
      }

      if (claim.status === "NeedsReview") {
        return { ...summary, needsReview: summary.needsReview + 1 };
      }

      return summary;
    },
    { supported: 0, unsupported: 0, needsReview: 0 }
  );
}

function parseJobSignals(value: string | undefined): JobSignalsDocument {
  return {
    provider: "Fake",
    extractedAt: "",
    requiredSkills: [],
    preferredSkills: [],
    responsibilities: [],
    signals: [],
    ...(parseJsonObject<Partial<JobSignalsDocument>>(value) ?? {})
  };
}

function parseClaimAudit(value: string | undefined): ClaimAudit {
  return {
    claims: [],
    ...(parseJsonObject<Partial<ClaimAudit>>(value) ?? {})
  };
}

function parseDraftQualityCheck(value: string | undefined): DraftQualityCheck {
  return {
    status: "Passed",
    issues: [],
    copiedSevenWordPhraseCount: 0,
    copiedPhraseThreshold: 0,
    ...(parseJsonObject<Partial<DraftQualityCheck>>(value) ?? {})
  };
}

function parseJsonArray<T>(value: string | undefined): T[] {
  if (!value) {
    return [];
  }

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
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

function pageTitle(view: View): string {
  switch (view) {
    case "profile":
      return "Profile";
    case "applications":
      return "Applications";
    case "settings":
      return "Settings";
    default:
      return "Application writing workspace";
  }
}

function disabledTitle(isDisabled: boolean, reason: string | null | undefined): string | undefined {
  return isDisabled && reason ? reason : undefined;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric"
  }).format(new Date(value));
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(new Date(value));
}

export default App;
