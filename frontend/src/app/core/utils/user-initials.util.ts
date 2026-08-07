export function getUserInitials(email?: string | null): string {
  if (!email?.trim()) {
    return '?';
  }

  const localPart = email.trim().split('@')[0] ?? '';
  const nameParts = localPart.split(/[._-]+/).filter((part) => part.length > 0);

  if (nameParts.length >= 2) {
    return `${nameParts[0][0]}${nameParts[1][0]}`.toUpperCase();
  }

  if (localPart.length >= 2) {
    return localPart.slice(0, 2).toUpperCase();
  }

  return localPart.slice(0, 1).toUpperCase() || '?';
}
