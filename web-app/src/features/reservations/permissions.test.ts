import { describe, expect, it } from 'vitest'
import { getReservationPermissions } from './permissions'

describe('reservation permissions', () => {
  it('blocks Grid Operator actions and standalone prosumer access', () => {
    expect(getReservationPermissions('GridOperator')).toEqual({
      canManageBookings: false,
      canApprove: false,
      canReject: false,
      canReadProsumers: false,
    })
  })

  it('preserves Backoffice administration', () => {
    expect(getReservationPermissions('Backoffice')).toEqual({
      canManageBookings: true,
      canApprove: true,
      canReject: true,
      canReadProsumers: true,
    })
  })

  it('grants no access without a signed-in role', () => {
    expect(getReservationPermissions(undefined)).toEqual({
      canManageBookings: false,
      canApprove: false,
      canReject: false,
      canReadProsumers: false,
    })
  })
})
