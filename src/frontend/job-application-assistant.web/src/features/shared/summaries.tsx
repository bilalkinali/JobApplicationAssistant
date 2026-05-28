import {
  applicationStrategySections,
  type ApplicationStrategy
} from "../../applicationStrategy";
import {
  candidateFitBriefSections,
  type CandidateFitBrief
} from "../../candidateFitBrief";
import {
  getProviderReadinessTitle,
  getProviderRecoveryGuidance,
  getProviderSummary,
  getReadinessTone,
  isFakeProvider
} from "../../readiness";
import { StatusBadge } from "../../components/StatusBadge";
import type { AiDiagnostics, AiProviderStatus } from "./types";

export function SignalColumn(props: { title: string; values: string[] }) {
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

export function CandidateFitBriefSummary(props: { brief: CandidateFitBrief }) {
  const sections = candidateFitBriefSections(props.brief);

  return (
    <section className="fit-brief-summary" aria-label="Candidate fit brief summary">
      <div className="section-heading">
        <div>
          <h4>Candidate fit brief</h4>
          <p>Read-only preparation context. Supporting fact references are traceability only, not approved evidence.</p>
        </div>
        <StatusBadge tone="neutral">Read-only</StatusBadge>
      </div>
      {props.brief.candidateSummary.trim() && (
        <p className="fit-brief-candidate-summary">{props.brief.candidateSummary}</p>
      )}
      {props.brief.skillGroups.length > 0 && (
        <div className="fit-brief-skill-groups">
          {props.brief.skillGroups.map((group, groupIndex) => (
            <article className="fit-brief-card" key={`${group.name}-${groupIndex}`}>
              <strong>{group.name}</strong>
              {group.items.length === 0 ? (
                <p className="empty-state compact">No skills listed.</p>
              ) : (
                <ul>
                  {group.items.map((item, itemIndex) => (
                    <li key={`${group.name}-${item.title}-${itemIndex}`}>
                      <span>{item.title}</span>
                      {item.summary && <small>{item.summary}</small>}
                    </li>
                  ))}
                </ul>
              )}
            </article>
          ))}
        </div>
      )}
      <div className="fit-brief-section-grid">
        {sections.map((section) => (
          <article className={`fit-brief-card ${section.tone}`} key={section.key}>
            <h5>{section.title}</h5>
            {section.items.length === 0 ? (
              <p className="empty-state compact">None recorded.</p>
            ) : (
              <ul>
                {section.items.map((item, itemIndex) => (
                  <li key={`${section.key}-${item.title}-${itemIndex}`}>
                    <span>{item.title}</span>
                    {item.summary && <small>{item.summary}</small>}
                  </li>
                ))}
              </ul>
            )}
          </article>
        ))}
      </div>
    </section>
  );
}

export function ApplicationStrategySummary(props: { strategy: ApplicationStrategy }) {
  const sections = applicationStrategySections(props.strategy);

  return (
    <section className="application-strategy-summary" aria-label="Application strategy summary">
      <div className="section-heading">
        <div>
          <h4>Application strategy</h4>
          <p>Read-only writing plan for the next generated draft.</p>
        </div>
        <StatusBadge tone="neutral">Read-only</StatusBadge>
      </div>
      {props.strategy.toneGuidance.trim() && (
        <p className="strategy-tone-guidance">{props.strategy.toneGuidance}</p>
      )}
      <div className="strategy-section-grid">
        {sections.map((section) => (
          <article className={`strategy-card ${section.tone}`} key={section.key}>
            <h5>{section.title}</h5>
            {section.items.length === 0 ? (
              <p className="empty-state compact">None recorded.</p>
            ) : (
              <ul>
                {section.items.map((item, itemIndex) => (
                  <li key={`${section.key}-${itemIndex}`}>{item}</li>
                ))}
              </ul>
            )}
          </article>
        ))}
      </div>
    </section>
  );
}

export function ProviderReadinessSummary({
  status,
  diagnostics,
  diagnosticsLastRanAt,
  compact = false
}: {
  status: AiProviderStatus;
  diagnostics: AiDiagnostics | null;
  diagnosticsLastRanAt: string | null;
  compact?: boolean;
}) {
  const tone = getReadinessTone(status);
  const details = diagnostics?.checks ?? [];

  return (
    <section className={`provider-readiness workflow-note ${tone}`}>
      <strong>{getProviderReadinessTitle(status)}</strong>
      <p>{getProviderSummary(status)}</p>
      {!status.isAvailable && !isFakeProvider(status) && (
        <p>{getProviderRecoveryGuidance(status)}</p>
      )}
      <dl className={`status-details ${compact ? "compact" : ""}`}>
        <div>
          <dt>Provider</dt>
          <dd>{status.provider}</dd>
        </div>
        <div>
          <dt>Mode</dt>
          <dd>{isFakeProvider(status) ? "Deterministic demo/test behavior" : "Configured real provider"}</dd>
        </div>
        <div>
          <dt>Model</dt>
          <dd>{status.model}</dd>
        </div>
        <div>
          <dt>Endpoint</dt>
          <dd>{status.endpoint ?? "Not applicable"}</dd>
        </div>
        <div>
          <dt>Availability</dt>
          <dd>{status.isAvailable ? "Available" : "Unavailable"}</dd>
        </div>
        <div>
          <dt>Diagnostics</dt>
          <dd>{diagnosticsLastRanAt ? `Last ran ${formatDateTime(diagnosticsLastRanAt)}` : "Not run this session"}</dd>
        </div>
      </dl>
      {details.length > 0 && (
        <details>
          <summary>Readiness checks</summary>
          <ul>
            {details.map((check) => (
              <li key={`${check.name}-${check.status}`}>
                <strong>{check.name}</strong>: {check.status} - {check.message}
              </li>
            ))}
          </ul>
        </details>
      )}
    </section>
  );
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(new Date(value));
}
