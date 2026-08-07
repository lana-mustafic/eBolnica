export function formatRelativeTime(value: string): string {
  const diffMs = Date.now() - new Date(value).getTime();
  const minutes = Math.floor(diffMs / 60000);

  if (minutes < 1) return 'upravo sada';
  if (minutes < 60) return `prije ${minutes}min`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `prije ${hours}h`;

  const days = Math.floor(hours / 24);
  return `prije ${days}d`;
}
