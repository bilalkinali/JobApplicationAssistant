import type { ErrorPresentation } from "../../errorPresentation";
import { getProviderSummary } from "../../readiness";
import { ErrorMessage } from "../../components/FeedbackMessages";
import type { AiDiagnostics, AiProviderStatus } from "../shared/types";

type SettingsViewProps = {
  aiStatus: AiProviderStatus | null;
  aiDiagnostics: AiDiagnostics | null;
  aiDiagnosticsError: ErrorPresentation | null;
  aiDiagnosticsBusy: boolean;
  aiDiagnosticsLastRanAt: string | null;
  onRunAiDiagnostics: () => void;
  formatDateTime: (value: string) => string;
};

export function SettingsView(props: SettingsViewProps) {
  return (
    <article className="panel settings-panel">
      <h3>AI settings</h3>
      {props.aiStatus ? (
        <>
          <p>{getProviderSummary(props.aiStatus)}</p>
          <dl className="status-details">
            <div>
              <dt>Provider</dt>
              <dd>{props.aiStatus.provider}</dd>
            </div>
            <div>
              <dt>Model</dt>
              <dd>{props.aiStatus.model}</dd>
            </div>
            <div>
              <dt>Endpoint</dt>
              <dd>{props.aiStatus.endpoint ?? "Not applicable"}</dd>
            </div>
            <div>
              <dt>Availability</dt>
              <dd>{props.aiStatus.isAvailable ? "Available" : "Unavailable"}</dd>
            </div>
            <div>
              <dt>Diagnostics</dt>
              <dd>{props.aiDiagnosticsLastRanAt ? `Last ran ${props.formatDateTime(props.aiDiagnosticsLastRanAt)}` : "Not run this session"}</dd>
            </div>
          </dl>
        </>
      ) : (
        <p>Loading AI provider status.</p>
      )}
      <button className="primary-action" type="button" onClick={props.onRunAiDiagnostics} disabled={props.aiDiagnosticsBusy}>
        {props.aiDiagnosticsBusy ? "Running diagnostics..." : "Run diagnostics"}
      </button>
      {props.aiDiagnosticsBusy && <p className="diagnostics-state">Checking provider connectivity and model readiness.</p>}
      {!props.aiDiagnosticsBusy && !props.aiDiagnostics && !props.aiDiagnosticsError && (
        <p className="diagnostics-state">Diagnostics have not been run this session.</p>
      )}
      {props.aiDiagnosticsError && <ErrorMessage error={props.aiDiagnosticsError} compact />}
      {props.aiDiagnostics && (
        <section className="diagnostics-list">
          <h4>Diagnostics result</h4>
          <p className={props.aiDiagnostics.isAvailable ? "diagnostics-state success" : "diagnostics-state warning"}>
            {props.aiDiagnostics.message}
          </p>
          {props.aiDiagnostics.checks.map((check) => (
            <article className="diagnostics-item" key={check.name}>
              <strong>{check.name}</strong>
              <span>{check.status}</span>
              <p>{check.message}</p>
            </article>
          ))}
        </section>
      )}
    </article>
  );
}
