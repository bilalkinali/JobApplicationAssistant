import { FormEvent, useEffect, useMemo, useState } from "react";
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
  createdAt: string;
  updatedAt: string;
};

type View = "home" | "profile" | "applications" | "settings";

function App() {
  const [view, setView] = useState<View>("home");
  const [profile, setProfile] = useState<ProfileForm>(emptyProfile);
  const [applications, setApplications] = useState<ApplicationSession[]>([]);
  const [applicationForm, setApplicationForm] = useState<ApplicationForm>(emptyApplication);
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const selectedApplication = useMemo(
    () => applications.find((application) => application.id === selectedApplicationId),
    [applications, selectedApplicationId]
  );

  useEffect(() => {
    void loadProfile();
    void loadApplications();
  }, []);

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
      const response = await apiGet<ApplicationSession[]>("/api/applications");
      setApplications(response.map(toApplicationForm));
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

  function openApplication(application: ApplicationSession) {
    setSelectedApplicationId(application.id);
    setApplicationForm(toApplicationForm(application));
    setView("applications");
    setError(null);
    setNotice(null);
  }

  function startNewApplication() {
    setSelectedApplicationId(null);
    setApplicationForm(emptyApplication);
    setView("applications");
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
          <span className="status-pill">CRUD foundation</span>
        </header>

        {error && <div className="message error">{error}</div>}
        {notice && <div className="message success">{notice}</div>}

        {view === "home" && (
          <div className="panel-grid">
            <article className="panel">
              <h3>Profile readiness</h3>
              <p>{profile.fullName ? `${profile.fullName} is ready for later profile fact work.` : "Profile contact details are not saved yet."}</p>
            </article>
            <article className="panel">
              <h3>New application</h3>
              <p>Create a basic application session before later analysis and drafting slices are added.</p>
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
              <p>AI providers and diagnostics are intentionally outside this slice.</p>
            </article>
          </div>
        )}

        {view === "profile" && (
          <form className="form-layout" onSubmit={saveProfile}>
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
        )}

        {view === "applications" && (
          <div className="applications-layout">
            <section className="list-panel" aria-label="Application sessions">
              <div className="list-header">
                <h3>Sessions</h3>
                <button type="button" onClick={startNewApplication}>New</button>
              </div>
              <div className="session-list">
                {applications.map((application) => (
                  <button
                    className={application.id === selectedApplicationId ? "session active" : "session"}
                    key={application.id}
                    type="button"
                    onClick={() => openApplication(application)}
                  >
                    <strong>{application.companyName}</strong>
                    <span>{application.roleTitle}</span>
                  </button>
                ))}
                {applications.length === 0 && <p className="empty-state">No application sessions yet.</p>}
              </div>
            </section>

            <form className="form-layout editor-panel" onSubmit={saveApplication}>
              <Field label="Company name" required value={applicationForm.companyName} onChange={(companyName) => setApplicationForm({ ...applicationForm, companyName })} />
              <Field label="Role title" required value={applicationForm.roleTitle} onChange={(roleTitle) => setApplicationForm({ ...applicationForm, roleTitle })} />
              <Field label="Application URL" type="url" value={applicationForm.applicationUrl} onChange={(applicationUrl) => setApplicationForm({ ...applicationForm, applicationUrl })} />
              <Field label="Deadline" type="date" value={applicationForm.deadline} onChange={(deadline) => setApplicationForm({ ...applicationForm, deadline })} />
              <Field label="Status" required value={applicationForm.status} onChange={(status) => setApplicationForm({ ...applicationForm, status })} />
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
            </form>
          </div>
        )}

        {view === "settings" && (
          <article className="panel">
            <h3>Settings</h3>
            <p>Provider settings and diagnostics are reserved for the later AI slices.</p>
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

function Textarea(props: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="field wide">
      <span>{props.label}</span>
      <textarea value={props.value} onChange={(event) => props.onChange(event.target.value)} />
    </label>
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

function toApplicationForm(application: ApplicationSession): ApplicationSession;
function toApplicationForm(application: ApplicationForm): ApplicationForm;
function toApplicationForm(application: ApplicationForm | ApplicationSession) {
  return {
    ...application,
    applicationUrl: application.applicationUrl ?? "",
    deadline: application.deadline ?? "",
    jobPostingText: application.jobPostingText ?? "",
    detectedLanguage: application.detectedLanguage ?? "",
    selectedLanguage: application.selectedLanguage ?? ""
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
    return "The API request failed.";
  }

  const details = apiError.details
    ? Object.entries(apiError.details)
        .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`))
        .join(" ")
    : "";

  return details ? `${apiError.message} ${details}` : apiError.message;
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

export default App;
