import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  getAuditExportWarning,
  getCoverLetterExportState,
  getCoverLetterText
} from "./exportControls";
import {
  getDraftGenerationState,
  getEvidenceMatchingState,
  getJobAnalysisState,
  getProfileReadiness
} from "./readiness";
import "./styles.css";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5108";
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
const applicationStatuses = ["Draft", "PostingCaptured", "ReadyForReview", "Applied", "Archived"];
const auditReadinessOptions = ["All", "Current", "Stale", "Missing", "NotApplicable"];
const profileFactStatuses = ["Draft", "Approved", "Archived"];

type ApiError = {
  code: string;
  message: string;
  details?: Record<string, string[]>;
};

type ProfileForm = {
  fullName: string;
  email: string;
  phone: string;
  location: string;
  linkedInUrl: string;
  gitHubUrl: string;
  portfolioUrl: string;
  defaultLanguage: string;
  danishTone: string;
  englishTone: string;
};

type ProfileFactForm = {
  type: string;
  title: string;
  summary: string;
  status: string;
  factItems: string;
  technologies: string;
  allowedClaims: string;
  forbiddenClaims: string;
};

type ProfileFact = ProfileFactForm & {
  id: string;
  createdAt: string;
  updatedAt: string;
};

type ApplicationForm = {
  companyName: string;
  roleTitle: string;
  applicationUrl: string;
  deadline: string;
  status: string;
  jobPostingText: string;
  detectedLanguage: string;
  selectedLanguage: string;
};

type ApplicationSession = ApplicationForm & {
  id: string;
  jobSignals: string;
  evidenceMatches: string;
  unmatchedRequirements: string;
  approvedEvidence: string;
  generatedDraft: GeneratedDraft | null;
  hasGeneratedDraft: boolean;
  auditReadiness: string;
  createdAt: string;
  updatedAt: string;
};

type GeneratedDraft = {
  id: string;
  jobApplicationId: string;
  coverLetterText: string;
  shortMotivationText: string;
  claimAudit: string;
  generatedAt: string;
  lastEditedAt: string | null;
  auditUpdatedAt: string | null;
  createdAt: string;
  updatedAt: string;
  isClaimAuditStale: boolean;
};

type GeneratedDraftForm = {
  coverLetterText: string;
  shortMotivationText: string;
};

type View = "home" | "profile" | "applications" | "settings";

type JobSignalsDocument = {
  provider: string;
  extractedAt: string;
  requiredSkills: string[];
  preferredSkills: string[];
  responsibilities: string[];
  signals: JobSignal[];
};

type JobSignal = {
  id: string;
  label: string;
  category: string;
  keywords: string[];
};

type EvidenceMatch = {
  id: string;
  signalId: string;
  signal: string;
  category: string;
  profileFactId: string;
  profileFactTitle: string;
  summary: string;
  matchedTerms: string[];
};

type UnmatchedRequirement = {
  id: string;
  signalId: string;
  requirement: string;
  category: string;
  recommendation: string;
};

type ClaimAudit = {
  claims: ClaimAuditClaim[];
};

type ClaimAuditClaim = {
  id: string;
  text: string;
  status: "Supported" | "Unsupported" | "NeedsReview" | string;
  evidenceIds: string[];
};

type AiProviderStatus = {
  provider: string;
  model: string;
  endpoint: string | null;
  isAvailable: boolean;
  message: string;
};

type AiDiagnostics = AiProviderStatus & {
  checks: AiDiagnosticCheck[];
};

type AiDiagnosticCheck = {
  name: string;
  status: string;
  message: string;
};

function App() {
  const [view, setView] = useState<View>("home");
  const [profile, setProfile] = useState<ProfileForm>(emptyProfile);
  const [profileFacts, setProfileFacts] = useState<ProfileFact[]>([]);
  const [profileFactForm, setProfileFactForm] = useState<ProfileFactForm>(emptyProfileFact);
  const [selectedProfileFactId, setSelectedProfileFactId] = useState<string | null>(null);
  const [applications, setApplications] = useState<ApplicationSession[]>([]);
  const [applicationForm, setApplicationForm] = useState<ApplicationForm>(emptyApplication);
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);
  const [applicationSearch, setApplicationSearch] = useState("");
  const [applicationStatusFilter, setApplicationStatusFilter] = useState("All");
  const [applicationReadinessFilter, setApplicationReadinessFilter] = useState("All");
  const [includeArchivedApplications, setIncludeArchivedApplications] = useState(false);
  const [approvedEvidenceDraft, setApprovedEvidenceDraft] = useState<EvidenceMatch[]>([]);
  const [generatedDraftForm, setGeneratedDraftForm] = useState<GeneratedDraftForm>({
    coverLetterText: "",
    shortMotivationText: ""
  });
  const [workflowBusy, setWorkflowBusy] = useState<string | null>(null);
  const [exportBusy, setExportBusy] = useState<"txt" | "docx" | null>(null);
  const [isClipboardAvailable, setIsClipboardAvailable] = useState(false);
  const [aiStatus, setAiStatus] = useState<AiProviderStatus | null>(null);
  const [aiDiagnostics, setAiDiagnostics] = useState<AiDiagnostics | null>(null);
  const [aiDiagnosticsError, setAiDiagnosticsError] = useState<string | null>(null);
  const [aiDiagnosticsBusy, setAiDiagnosticsBusy] = useState(false);
  const [aiDiagnosticsLastRanAt, setAiDiagnosticsLastRanAt] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const selectedApplication = useMemo(
    () => applications.find((application) => application.id === selectedApplicationId),
    [applications, selectedApplicationId]
  );
  const approvedProfileFacts = useMemo(
    () => profileFacts.filter((fact) => fact.status === "Approved"),
    [profileFacts]
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
  const claimAudit = useMemo(
    () => parseClaimAudit(selectedApplication?.generatedDraft?.claimAudit),
    [selectedApplication?.generatedDraft?.claimAudit]
  );
  const hasSavedJobPosting = Boolean(selectedApplication?.jobPostingText.trim());
  const hasSavedApprovedEvidence = savedApprovedEvidence.length > 0;
  const hasGeneratedDraft = Boolean(selectedApplication?.generatedDraft);
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
  const auditExportWarning = useMemo(
    () => getAuditExportWarning(selectedApplication),
    [selectedApplication]
  );
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
        hasSavedApprovedEvidence
      }),
    [hasSavedApprovedEvidence, hasSavedJobPosting, selectedApplicationId]
  );

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
    setGeneratedDraftForm({
      coverLetterText: selectedApplication?.generatedDraft?.coverLetterText ?? "",
      shortMotivationText: selectedApplication?.generatedDraft?.shortMotivationText ?? ""
    });
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

  async function loadProfileFacts() {
    try {
      const response = await apiGet<ProfileFact[]>("/api/profile/facts");
      setProfileFacts(response.map(toProfileFact));
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

    setError(null);
    setNotice(null);

    try {
      await apiDelete(`/api/profile/facts/${selectedProfileFactId}`);
      startNewProfileFact();
      await loadProfileFacts();
      setNotice("Profile fact deleted.");
    } catch (apiError) {
      setError(formatError(apiError));
    }
  }

  async function deleteApplication() {
    if (!selectedApplicationId) {
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
      setError("Save the application before changing its final status.");
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
      setError("Save the application before running job analysis.");
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
      setError("Save the application before matching evidence.");
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

  async function saveApprovedEvidence() {
    if (!selectedApplicationId) {
      setError("Save the application before reviewing evidence.");
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("review");

    try {
      const saved = await apiSend<ApplicationSession>(
        `/api/applications/${selectedApplicationId}/approved-evidence`,
        "PUT",
        { approvedEvidence: JSON.stringify(approvedEvidenceDraft) }
      );
      replaceApplication(saved);
      setNotice("Approved evidence saved.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function generateDraft() {
    if (!selectedApplicationId) {
      setError("Save the application before generating a draft.");
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("draft");

    try {
      const draft = await apiSend<GeneratedDraft>(`/api/applications/${selectedApplicationId}/generate-draft`, "POST", null);
      replaceGeneratedDraft(draft);
      setNotice("Draft generated.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function saveGeneratedDraft() {
    if (!selectedApplicationId) {
      setError("Save the application before editing a draft.");
      return;
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
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function auditClaims() {
    if (!selectedApplicationId) {
      setError("Save the application before auditing a draft.");
      return;
    }

    setError(null);
    setNotice(null);
    setWorkflowBusy("audit");

    try {
      const draft = await apiSend<GeneratedDraft>(`/api/applications/${selectedApplicationId}/audit-claims`, "POST", null);
      replaceGeneratedDraft(draft);
      setNotice("Claim audit updated.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setWorkflowBusy(null);
    }
  }

  async function copyCoverLetter() {
    if (!coverLetterExportState.canCopy) {
      setError(coverLetterExportState.reason ?? "Clipboard copy is not available in this browser.");
      return;
    }

    setError(null);
    setNotice(null);

    try {
      await navigator.clipboard.writeText(getCoverLetterText(selectedApplication, generatedDraftForm.coverLetterText));
      setNotice("Cover letter copied.");
    } catch {
      setError("Clipboard copy failed. You can still select and copy the cover letter manually.");
    }
  }

  async function downloadCoverLetter(format: "txt" | "docx") {
    if (!selectedApplicationId || !coverLetterExportState.canExport) {
      setError(coverLetterExportState.reason ?? "Select an application with a generated cover letter before exporting.");
      return;
    }

    setError(null);
    setNotice(null);
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
      setNotice(format === "txt" ? "TXT cover letter downloaded." : "DOCX cover letter downloaded.");
    } catch (apiError) {
      setError(formatError(apiError));
    } finally {
      setExportBusy(null);
    }
  }

  async function runAiDiagnostics() {
    setError(null);
    setNotice(null);
    setAiDiagnosticsError(null);
    setAiDiagnostics(null);
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
    setApprovedEvidenceDraft((current) =>
      current.some((item) => item.id === match.id) ? current : [...current, match]
    );
  }

  function removeApprovedEvidence(matchId: string) {
    setApprovedEvidenceDraft((current) => current.filter((item) => item.id !== matchId));
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
    setError(null);
    setNotice(null);
  }

  function startNewProfileFact() {
    setSelectedProfileFactId(null);
    setProfileFactForm(emptyProfileFact);
    setError(null);
    setNotice(null);
  }

  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="brand">
          <span className="brand-mark">JA</span>
          <div>
            <p className="eyebrow">V1 workspace</p>
            <h1>Job Application Assistant</h1>
          </div>
        </div>

        <nav className="navigation">
          {(["home", "profile", "applications", "settings"] satisfies View[]).map((item) => (
            <button
              className={view === item ? "active" : ""}
              key={item}
              type="button"
              onClick={() => setView(item)}
            >
              {titleCase(item)}
            </button>
          ))}
        </nav>
      </aside>

      <section className="workspace" aria-labelledby="workspace-title">
        <header className="workspace-header">
          <div>
            <p className="eyebrow">Workbench</p>
            <h2 id="workspace-title">{pageTitle(view)}</h2>
          </div>
          <span className={`status-pill ${aiStatus?.isAvailable === false ? "unavailable" : ""}`}>
            {aiStatus ? `${aiStatus.provider} - ${aiStatus.model} - ${availabilityLabel(aiStatus)}` : "AI status loading"}
          </span>
        </header>

        {error && <div className="message error">{error}</div>}
        {notice && <div className="message success">{notice}</div>}

        {view === "home" && (
          <div className="panel-grid">
            <article className="panel">
              <h3>Profile readiness</h3>
              <p>{profileReadiness.contactMessage}</p>
              <p>{profileReadiness.evidenceMessage}</p>
              {profileReadiness.warnings.length > 0 && (
                <button type="button" onClick={() => setView("profile")}>
                  Review profile setup
                </button>
              )}
            </article>
            <article className="panel">
              <h3>New application</h3>
              <p>Create a saved application workspace for the posting, language, and manual workflow state.</p>
              <button className="primary-action" type="button" onClick={startNewApplication}>
                New application
              </button>
            </article>
            <article className="panel">
              <h3>Recent applications</h3>
              <p>{applications.length} application session{applications.length === 1 ? "" : "s"} saved.</p>
            </article>
            <article className="panel">
              <h3>AI status</h3>
              {aiStatus ? (
                <>
                  <p>{providerSummary(aiStatus)}</p>
                  <dl className="status-details compact">
                    <div>
                      <dt>Provider</dt>
                      <dd>{aiStatus.provider}</dd>
                    </div>
                    <div>
                      <dt>Model</dt>
                      <dd>{aiStatus.model}</dd>
                    </div>
                    <div>
                      <dt>Availability</dt>
                      <dd>{aiStatus.isAvailable ? "Available" : "Unavailable"}</dd>
                    </div>
                    <div>
                      <dt>Diagnostics</dt>
                      <dd>{aiDiagnosticsLastRanAt ? `Last ran ${formatDateTime(aiDiagnosticsLastRanAt)}` : "Not run this session"}</dd>
                    </div>
                  </dl>
                </>
              ) : (
                <p>Loading AI provider status.</p>
              )}
            </article>
          </div>
        )}

        {view === "profile" && (
          <div className="profile-layout">
            <form className="form-layout panel-form" onSubmit={saveProfile}>
              <div className="section-heading">
                <h3>Contact and tone</h3>
                <p>{profileReadiness.contactMessage}</p>
              </div>
              {!profileReadiness.hasContactDetails && (
                <p className="workflow-note warning">Contact setup is incomplete. You can keep editing applications, but later drafts and exports may miss useful applicant context.</p>
              )}
              <Field label="Full name" required value={profile.fullName} onChange={(fullName) => setProfile({ ...profile, fullName })} />
              <Field label="Email" required type="email" value={profile.email} onChange={(email) => setProfile({ ...profile, email })} />
              <Field label="Phone" value={profile.phone} onChange={(phone) => setProfile({ ...profile, phone })} />
              <Field label="Location" value={profile.location} onChange={(location) => setProfile({ ...profile, location })} />
              <Field label="LinkedIn URL" type="url" value={profile.linkedInUrl} onChange={(linkedInUrl) => setProfile({ ...profile, linkedInUrl })} />
              <Field label="GitHub URL" type="url" value={profile.gitHubUrl} onChange={(gitHubUrl) => setProfile({ ...profile, gitHubUrl })} />
              <Field label="Portfolio URL" type="url" value={profile.portfolioUrl} onChange={(portfolioUrl) => setProfile({ ...profile, portfolioUrl })} />
              <Field label="Default language" required value={profile.defaultLanguage} onChange={(defaultLanguage) => setProfile({ ...profile, defaultLanguage })} />
              <Textarea label="Danish tone" value={profile.danishTone} onChange={(danishTone) => setProfile({ ...profile, danishTone })} />
              <Textarea label="English tone" value={profile.englishTone} onChange={(englishTone) => setProfile({ ...profile, englishTone })} />
              <div className="form-actions">
                <button className="primary-action" type="submit">Save profile</button>
              </div>
            </form>

            <section className="profile-facts-panel">
              <div className="section-heading">
                <h3>Profile facts</h3>
                <p>{profileReadiness.evidenceMessage}</p>
              </div>
              {!profileReadiness.hasApprovedEvidence && (
                <p className="workflow-note warning">Approved evidence is required before evidence matching and draft generation. Draft or archived facts will not be used as proof.</p>
              )}
              {profileFacts.length === 0 && <p className="empty-state">No profile facts yet.</p>}
              <div className="fact-list">
                {profileFacts.map((fact) => (
                  <button
                    className={fact.id === selectedProfileFactId ? "fact-card active" : `fact-card ${fact.status.toLowerCase()}`}
                    key={fact.id}
                    type="button"
                    onClick={() => openProfileFact(fact)}
                  >
                    <strong>{fact.title}</strong>
                    <span>{fact.type} - {fact.status}</span>
                  </button>
                ))}
              </div>

              <form className="form-layout fact-editor" onSubmit={saveProfileFact}>
                <Field label="Type" required value={profileFactForm.type} onChange={(type) => setProfileFactForm({ ...profileFactForm, type })} />
                <Select label="Status" required value={profileFactForm.status} options={profileFactStatuses} onChange={(status) => setProfileFactForm({ ...profileFactForm, status })} />
                <Field label="Title" required value={profileFactForm.title} onChange={(title) => setProfileFactForm({ ...profileFactForm, title })} />
                <Textarea label="Summary" required value={profileFactForm.summary} onChange={(summary) => setProfileFactForm({ ...profileFactForm, summary })} />
                <Textarea label="Fact items JSON" value={profileFactForm.factItems} onChange={(factItems) => setProfileFactForm({ ...profileFactForm, factItems })} />
                <Textarea label="Technologies JSON" value={profileFactForm.technologies} onChange={(technologies) => setProfileFactForm({ ...profileFactForm, technologies })} />
                <Textarea label="Allowed claims JSON" value={profileFactForm.allowedClaims} onChange={(allowedClaims) => setProfileFactForm({ ...profileFactForm, allowedClaims })} />
                <Textarea label="Forbidden claims JSON" value={profileFactForm.forbiddenClaims} onChange={(forbiddenClaims) => setProfileFactForm({ ...profileFactForm, forbiddenClaims })} />
                <div className="form-actions">
                  <button className="primary-action" type="submit">{selectedProfileFactId ? "Save fact" : "Create fact"}</button>
                  <button type="button" onClick={startNewProfileFact}>Clear</button>
                  {selectedProfileFactId && (
                    <button className="danger-action" type="button" onClick={deleteProfileFact}>Delete</button>
                  )}
                </div>
              </form>
            </section>
          </div>
        )}

        {view === "applications" && (
          <div className="applications-layout">
            <section className="list-panel" aria-label="Application sessions">
              <div className="list-header">
                <h3>Applications</h3>
                <button type="button" onClick={startNewApplication}>New</button>
              </div>
              <div className="filters">
                <Field label="Search" value={applicationSearch} onChange={setApplicationSearch} />
                <Select label="Status" value={applicationStatusFilter} options={["All", ...applicationStatuses]} onChange={setApplicationStatusFilter} />
                <Select label="Draft/audit" value={applicationReadinessFilter} options={auditReadinessOptions} onChange={setApplicationReadinessFilter} />
                <label className="checkbox-field">
                  <input
                    type="checkbox"
                    checked={includeArchivedApplications}
                    onChange={(event) => setIncludeArchivedApplications(event.target.checked)}
                  />
                  <span>Include archived</span>
                </label>
              </div>
              <div className="session-list">
                {filteredApplications.map((application) => (
                  <button
                    className={application.id === selectedApplicationId ? "session active" : "session"}
                    key={application.id}
                    type="button"
                    onClick={() => void openApplication(application)}
                  >
                    <strong>{application.companyName}</strong>
                    <span>{application.roleTitle}</span>
                    <small>
                      {application.status} - {application.selectedLanguage || application.detectedLanguage || "Language unset"} - Updated {formatDate(application.updatedAt)}
                    </small>
                    <div className="session-badges">
                      {application.deadline && <span>Deadline {formatDate(application.deadline)}</span>}
                      <span className={application.hasGeneratedDraft ? "ready" : "muted"}>{application.hasGeneratedDraft ? "Draft ready" : "No draft"}</span>
                      <span className={`audit-${application.auditReadiness.toLowerCase()}`}>Audit {readinessLabel(application.auditReadiness)}</span>
                    </div>
                  </button>
                ))}
                {applications.length === 0 && <p className="empty-state">No application sessions yet.</p>}
                {applications.length > 0 && filteredApplications.length === 0 && <p className="empty-state">No applications match the current filters.</p>}
              </div>
            </section>

            <form className="form-layout editor-panel" onSubmit={saveApplication}>
              <div className="section-heading">
                <h3>{selectedApplication ? "Application detail" : "New application"}</h3>
                <p>Capture the posting, language, and manual workflow state for this session.</p>
              </div>
              <Field label="Company name" required value={applicationForm.companyName} onChange={(companyName) => setApplicationForm({ ...applicationForm, companyName })} />
              <Field label="Role title" required value={applicationForm.roleTitle} onChange={(roleTitle) => setApplicationForm({ ...applicationForm, roleTitle })} />
              <Field label="Application URL" type="url" value={applicationForm.applicationUrl} onChange={(applicationUrl) => setApplicationForm({ ...applicationForm, applicationUrl })} />
              <Field label="Deadline" type="date" value={applicationForm.deadline} onChange={(deadline) => setApplicationForm({ ...applicationForm, deadline })} />
              <Select label="Status" required value={applicationForm.status} options={applicationStatuses} onChange={(status) => setApplicationForm({ ...applicationForm, status })} />
              <Field label="Detected language" value={applicationForm.detectedLanguage} onChange={(detectedLanguage) => setApplicationForm({ ...applicationForm, detectedLanguage })} />
              <Field label="Selected language" value={applicationForm.selectedLanguage} onChange={(selectedLanguage) => setApplicationForm({ ...applicationForm, selectedLanguage })} />
              <Textarea label="Job posting text" value={applicationForm.jobPostingText} onChange={(jobPostingText) => setApplicationForm({ ...applicationForm, jobPostingText })} />
              <div className="form-actions">
                <button className="primary-action" type="submit">{selectedApplication ? "Save application" : "Create application"}</button>
                <button type="button" onClick={startNewApplication}>Clear</button>
                {selectedApplicationId && (
                  <button className="danger-action" type="button" onClick={deleteApplication}>Delete</button>
                )}
              </div>
              {selectedApplicationId && (
                <div className="final-status-actions" aria-label="Final application status actions">
                  <button
                    type="button"
                    onClick={() => void markApplicationStatus("Applied")}
                    disabled={selectedApplication?.status === "Applied" || workflowBusy !== null}
                  >
                    Mark applied
                  </button>
                  <button
                    className="danger-action"
                    type="button"
                    onClick={() => void markApplicationStatus("Archived")}
                    disabled={selectedApplication?.status === "Archived" || workflowBusy !== null}
                  >
                    Archive
                  </button>
                </div>
              )}

              <section className="workflow-panel">
                <div className="section-heading">
                  <h3>Application text workflow</h3>
                  <p>Move from job analysis to approved evidence, generated text, and claim audit before final use.</p>
                </div>
                {aiStatus && (
                  <p className={`workflow-note ${aiStatus.isAvailable ? "info" : "warning"}`}>
                    {aiWorkflowStatusMessage(aiStatus)}
                  </p>
                )}

                <div className="workflow-step">
                  <div>
                    <h4>1. Job analysis</h4>
                    <p>{jobAnalysisState.message}</p>
                  </div>
                  <button type="button" onClick={analyzeJob} disabled={!jobAnalysisState.canRun || workflowBusy !== null}>
                    {workflowBusy === "analysis" ? "Analyzing..." : "Analyze job"}
                  </button>
                </div>

                {jobSignals.signals.length > 0 ? (
                  <div className="signal-grid">
                    <SignalColumn title="Required skills" values={jobSignals.requiredSkills} />
                    <SignalColumn title="Preferred skills" values={jobSignals.preferredSkills} />
                    <SignalColumn title="Responsibilities" values={jobSignals.responsibilities} />
                  </div>
                ) : (
                  <p className="empty-state compact">No analysis results yet.</p>
                )}

                <div className="workflow-step">
                  <div>
                    <h4>2. Evidence matching</h4>
                    <p>{evidenceMatchingState.message}</p>
                  </div>
                  <button type="button" onClick={matchEvidence} disabled={!evidenceMatchingState.canRun || workflowBusy !== null}>
                    {workflowBusy === "matching" ? "Matching..." : "Match evidence"}
                  </button>
                </div>

                <div className="review-grid">
                  <section className="review-column">
                    <h4>Matched evidence</h4>
                    {evidenceMatches.length === 0 && <p className="empty-state compact">No matches yet.</p>}
                    {evidenceMatches.map((match) => (
                      <article className="evidence-card" key={match.id}>
                        <strong>{match.signal}</strong>
                        <span>{match.profileFactTitle}</span>
                        <p>{match.summary}</p>
                        <small>Matched: {match.matchedTerms.join(", ")}</small>
                        <button type="button" onClick={() => approveMatch(match)}>Approve</button>
                      </article>
                    ))}
                  </section>

                  <section className="review-column">
                    <h4>Unmatched requirements</h4>
                    {unmatchedRequirements.length === 0 && <p className="empty-state compact">No unmatched requirements recorded.</p>}
                    {unmatchedRequirements.map((requirement) => (
                      <article className="evidence-card muted" key={requirement.id}>
                        <strong>{requirement.requirement}</strong>
                        <span>{requirement.category}</span>
                        <p>{requirement.recommendation}</p>
                      </article>
                    ))}
                  </section>
                </div>

                <div className="workflow-step">
                  <div>
                    <h4>3. Approved evidence</h4>
                    <p>Save only the matched evidence that should be available to later generation steps.</p>
                  </div>
                  <button type="button" onClick={saveApprovedEvidence} disabled={!selectedApplicationId || workflowBusy !== null}>
                    {workflowBusy === "review" ? "Saving..." : "Save approved evidence"}
                  </button>
                </div>

                <div className="approved-list">
                  {approvedEvidenceDraft.length === 0 && <p className="empty-state compact">No approved evidence selected.</p>}
                  {approvedEvidenceDraft.map((match) => (
                    <article className="approved-item" key={match.id}>
                      <div>
                        <strong>{match.signal}</strong>
                        <span>{match.profileFactTitle}</span>
                      </div>
                      <button type="button" onClick={() => removeApprovedEvidence(match.id)}>Remove</button>
                    </article>
                  ))}
                </div>

                <div className="workflow-step">
                  <div>
                    <h4>4. Generated draft</h4>
                    <p>{draftGenerationState.message}</p>
                  </div>
                  <button type="button" onClick={generateDraft} disabled={!draftGenerationState.canRun || workflowBusy !== null}>
                    {workflowBusy === "draft" ? "Generating..." : "Generate draft"}
                  </button>
                </div>

                {selectedApplication?.generatedDraft ? (
                  <section className="draft-editor">
                    <div className="section-heading">
                      <h4>Current draft</h4>
                      <p>
                        Generated {formatDate(selectedApplication.generatedDraft.generatedAt)}
                        {selectedApplication.generatedDraft.lastEditedAt ? ` - Edited ${formatDate(selectedApplication.generatedDraft.lastEditedAt)}` : ""}
                        {selectedApplication.generatedDraft.isClaimAuditStale ? " - Audit stale" : ""}
                      </p>
                    </div>
                    {selectedApplication.generatedDraft.isClaimAuditStale && (
                      <p className="workflow-note warning">Draft edits were saved after the last audit. Run claim audit again before using this text.</p>
                    )}
                    <Textarea
                      label="Cover letter"
                      value={generatedDraftForm.coverLetterText}
                      onChange={(coverLetterText) => setGeneratedDraftForm({ ...generatedDraftForm, coverLetterText })}
                    />
                    <Textarea
                      label="Short motivation"
                      value={generatedDraftForm.shortMotivationText}
                      onChange={(shortMotivationText) => setGeneratedDraftForm({ ...generatedDraftForm, shortMotivationText })}
                    />
                    <div className="form-actions">
                      <button className="primary-action" type="button" onClick={saveGeneratedDraft} disabled={workflowBusy !== null}>
                        {workflowBusy === "draft-edit" ? "Saving..." : "Save draft edits"}
                      </button>
                      <button type="button" onClick={auditClaims} disabled={workflowBusy !== null}>
                        {workflowBusy === "audit" ? "Auditing..." : "Run claim audit"}
                      </button>
                    </div>
                    <section className="export-panel">
                      <div className="section-heading">
                        <h4>Export cover letter</h4>
                        <p>{coverLetterExportState.reason ?? "Copy or download the current cover letter exactly as edited."}</p>
                      </div>
                      {auditExportWarning && <p className="workflow-note warning">{auditExportWarning}</p>}
                      {!coverLetterExportState.canCopy && coverLetterExportState.canExport && (
                        <p className="workflow-note neutral">Clipboard copy is not available in this browser. TXT and DOCX export are still available.</p>
                      )}
                      <div className="form-actions">
                        <button type="button" onClick={() => void copyCoverLetter()} disabled={!coverLetterExportState.canCopy || exportBusy !== null}>
                          Copy
                        </button>
                        <button type="button" onClick={() => void downloadCoverLetter("txt")} disabled={!coverLetterExportState.canExport || exportBusy !== null}>
                          {exportBusy === "txt" ? "Downloading..." : "Download TXT"}
                        </button>
                        <button type="button" onClick={() => void downloadCoverLetter("docx")} disabled={!coverLetterExportState.canExport || exportBusy !== null}>
                          {exportBusy === "docx" ? "Downloading..." : "Download DOCX"}
                        </button>
                      </div>
                    </section>
                    <section className="claim-audit">
                      <div className="section-heading">
                        <h4>Claim audit</h4>
                        <p>
                          {selectedApplication.generatedDraft.auditUpdatedAt
                            ? `Updated ${formatDate(selectedApplication.generatedDraft.auditUpdatedAt)} - ${auditSummary.supported} supported, ${auditSummary.unsupported} unsupported, ${auditSummary.needsReview} needs review`
                            : "Run claim audit after the generated text is ready."}
                        </p>
                      </div>
                      {claimAudit.claims.length === 0 ? (
                        <p className="empty-state compact">No claim audit results yet.</p>
                      ) : (
                        <div className="audit-list">
                          {claimAudit.claims.map((claim) => (
                            <article className={`audit-item ${claim.status.toLowerCase()}`} key={claim.id}>
                              <span>{claim.status}</span>
                              <p>{claim.text}</p>
                              {claim.evidenceIds.length > 0 && <small>Evidence: {claim.evidenceIds.join(", ")}</small>}
                            </article>
                          ))}
                        </div>
                      )}
                    </section>
                  </section>
                ) : (
                  <section className="export-panel disabled">
                    <div className="section-heading">
                      <h4>Export cover letter</h4>
                      <p>{coverLetterExportState.reason ?? claimAuditMessage(hasGeneratedDraft)}</p>
                    </div>
                    <div className="form-actions">
                      <button type="button" disabled>Copy</button>
                      <button type="button" disabled>Download TXT</button>
                      <button type="button" disabled>Download DOCX</button>
                    </div>
                  </section>
                )}
              </section>
            </form>
          </div>
        )}

        {view === "settings" && (
          <article className="panel settings-panel">
            <h3>AI settings</h3>
            {aiStatus ? (
              <>
                <p>{providerSummary(aiStatus)}</p>
                <dl className="status-details">
                  <div>
                    <dt>Provider</dt>
                    <dd>{aiStatus.provider}</dd>
                  </div>
                  <div>
                    <dt>Model</dt>
                    <dd>{aiStatus.model}</dd>
                  </div>
                  <div>
                    <dt>Endpoint</dt>
                    <dd>{aiStatus.endpoint ?? "Not applicable"}</dd>
                  </div>
                  <div>
                    <dt>Availability</dt>
                    <dd>{aiStatus.isAvailable ? "Available" : "Unavailable"}</dd>
                  </div>
                  <div>
                    <dt>Diagnostics</dt>
                    <dd>{aiDiagnosticsLastRanAt ? `Last ran ${formatDateTime(aiDiagnosticsLastRanAt)}` : "Not run this session"}</dd>
                  </div>
                </dl>
              </>
            ) : (
              <p>Loading AI provider status.</p>
            )}
            <button className="primary-action" type="button" onClick={runAiDiagnostics} disabled={aiDiagnosticsBusy}>
              {aiDiagnosticsBusy ? "Running diagnostics..." : "Run diagnostics"}
            </button>
            {aiDiagnosticsBusy && <p className="diagnostics-state">Checking provider connectivity and model readiness.</p>}
            {!aiDiagnosticsBusy && !aiDiagnostics && !aiDiagnosticsError && (
              <p className="diagnostics-state">Diagnostics have not been run this session.</p>
            )}
            {aiDiagnosticsError && <p className="diagnostics-state error">{aiDiagnosticsError}</p>}
            {aiDiagnostics && (
              <section className="diagnostics-list">
                <h4>Diagnostics result</h4>
                <p className={aiDiagnostics.isAvailable ? "diagnostics-state success" : "diagnostics-state warning"}>
                  {aiDiagnostics.message}
                </p>
                {aiDiagnostics.checks.map((check) => (
                  <article className="diagnostics-item" key={check.name}>
                    <strong>{check.name}</strong>
                    <span>{check.status}</span>
                    <p>{check.message}</p>
                  </article>
                ))}
              </section>
            )}
          </article>
        )}
      </section>
    </main>
  );
}

function Field(props: {
  label: string;
  type?: string;
  required?: boolean;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="field">
      <span>{props.label}</span>
      <input
        type={props.type ?? "text"}
        required={props.required}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </label>
  );
}

function Select(props: {
  label: string;
  required?: boolean;
  value: string;
  options: string[];
  onChange: (value: string) => void;
}) {
  return (
    <label className="field">
      <span>{props.label}</span>
      <select
        required={props.required}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      >
        {props.options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
    </label>
  );
}

function Textarea(props: {
  label: string;
  required?: boolean;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="field wide">
      <span>{props.label}</span>
      <textarea required={props.required} value={props.value} onChange={(event) => props.onChange(event.target.value)} />
    </label>
  );
}

function SignalColumn(props: { title: string; values: string[] }) {
  return (
    <section className="signal-column">
      <h4>{props.title}</h4>
      {props.values.length === 0 ? (
        <p>None found.</p>
      ) : (
        <ul>
          {props.values.map((value) => (
            <li key={value}>{value}</li>
          ))}
        </ul>
      )}
    </section>
  );
}

async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`);
  return readResponse<T>(response);
}

async function apiSend<T>(path: string, method: "POST" | "PUT", body: unknown): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return readResponse<T>(response);
}

async function apiDelete(path: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method: "DELETE" });
  if (!response.ok) {
    throw await response.json();
  }
}

async function readResponse<T>(response: Response): Promise<T> {
  const body = await response.json();
  if (!response.ok) {
    throw body;
  }
  return body;
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
    updatedAt: fact.updatedAt
  };
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
    approvedEvidence: application.approvedEvidence || "[]",
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

function formatError(error: unknown): string {
  const apiError = error as ApiError;
  if (!apiError?.message) {
    return error instanceof Error ? error.message : "The API request failed.";
  }

  const aiProviderMessages = apiError.details?.AiProvider;
  if (aiProviderMessages?.length) {
    const providerMessage = aiProviderMessages.join(" ");
    if (providerMessage.toLowerCase().includes("unavailable")) {
      return `AI provider unavailable: ${providerMessage} Check AI settings, then run diagnostics. Existing workflow state was kept.`;
    }

    if (isInvalidAiOutputMessage(providerMessage)) {
      return `AI provider returned invalid output: ${providerMessage} Existing workflow state was kept.`;
    }

    return `AI provider failed: ${providerMessage} Existing workflow state was kept.`;
  }

  const details = apiError.details
    ? Object.entries(apiError.details)
        .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`))
        .join(" ")
    : "";

  return details ? `${apiError.message} ${details}` : apiError.message;
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

function providerSummary(status: AiProviderStatus): string {
  const availability = status.isAvailable ? "available" : "unavailable";
  const endpoint = status.endpoint ? ` at ${status.endpoint}` : "";
  const deterministic = isFakeProvider(status) ? " Deterministic fake workflow is active." : "";

  return `${status.provider} provider is ${availability} with model ${status.model}${endpoint}. ${status.message}${deterministic}`;
}

function availabilityLabel(status: AiProviderStatus): string {
  return status.isAvailable ? "Available" : "Unavailable";
}

function aiWorkflowStatusMessage(status: AiProviderStatus): string {
  if (isFakeProvider(status)) {
    return "Deterministic fake AI is active for repeatable workflow checks.";
  }

  if (status.isAvailable) {
    return `${status.provider} model ${status.model} is available for AI workflow actions.`;
  }

  return `${status.provider} model ${status.model} is unavailable. AI actions may fail until diagnostics pass; validation blockers are still shown separately.`;
}

function isFakeProvider(status: AiProviderStatus): boolean {
  return status.provider.toLowerCase() === "fake";
}

function isInvalidAiOutputMessage(message: string): boolean {
  const normalized = message.toLowerCase();

  return (
    normalized.includes("invalid") ||
    normalized.includes("malformed") ||
    normalized.includes("empty response") ||
    normalized.includes("incomplete") ||
    normalized.includes("duplicate")
  );
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

function titleCase(value: string): string {
  return `${value.slice(0, 1).toUpperCase()}${value.slice(1)}`;
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
