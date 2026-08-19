// Must match the backend's per-message history length cap.
export const HISTORY_MESSAGE_MAX_CHARS = 20000

const TRUNCATION_SUFFIX = '…[truncated]'

export function truncateForHistory(content: string): string {
  if (content.length <= HISTORY_MESSAGE_MAX_CHARS) return content
  return content.slice(0, HISTORY_MESSAGE_MAX_CHARS - TRUNCATION_SUFFIX.length) + TRUNCATION_SUFFIX
}
