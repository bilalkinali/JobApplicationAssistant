import type { ReactNode } from "react";
import type { ErrorPresentation } from "../errorPresentation";
import { getAvailabilityLabel, isFakeProvider } from "../readiness";
import type { AiProviderStatus, View } from "../features/shared/types";
import { ErrorMessage } from "./FeedbackMessages";

type AppShellProps = {
  view: View;
  onViewChange: (view: View) => void;
  title: string;
  aiStatus: AiProviderStatus | null;
  error: ErrorPresentation | null;
  notice: string | null;
  children: ReactNode;
};

export function AppShell(props: AppShellProps) {
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
              className={props.view === item ? "active" : ""}
              key={item}
              type="button"
              onClick={() => props.onViewChange(item)}
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
            <h2 id="workspace-title">{props.title}</h2>
          </div>
          <span
            className={`status-pill ${props.aiStatus?.isAvailable === false ? "unavailable" : ""} ${
              props.aiStatus && isFakeProvider(props.aiStatus) ? "fake" : ""
            }`}
          >
            {props.aiStatus
              ? `${props.aiStatus.provider} - ${props.aiStatus.model} - ${getAvailabilityLabel(props.aiStatus)}`
              : "AI status loading"}
          </span>
        </header>

        {props.error && <ErrorMessage error={props.error} />}
        {props.notice && (
          <div className="message success" role="status">
            <strong>Success</strong>
            <p>{props.notice}</p>
          </div>
        )}

        {props.children}
      </section>
    </main>
  );
}

function titleCase(value: string): string {
  return `${value.slice(0, 1).toUpperCase()}${value.slice(1)}`;
}
