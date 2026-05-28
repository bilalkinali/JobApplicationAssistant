export function StatusBadge(props: { tone: string; children: string }) {
  return <span className={`state-badge ${props.tone}`}>{props.children}</span>;
}
