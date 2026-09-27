/** Self-account guardrails mirrored from the backend's Backoffice safety checks (see UserService). */

interface IdentifiedUser {
  id: string
}

export function isCurrentUser(currentUserId: string | undefined, user: IdentifiedUser): boolean {
  return Boolean(currentUserId) && currentUserId === user.id
}

/** An administrator cannot deactivate their own account. */
export function canDeactivateUser(currentUserId: string | undefined, user: IdentifiedUser): boolean {
  return !isCurrentUser(currentUserId, user)
}

/** An administrator cannot change their own role, so the edit form locks it. */
export function canChangeRole(currentUserId: string | undefined, user: IdentifiedUser): boolean {
  return !isCurrentUser(currentUserId, user)
}
