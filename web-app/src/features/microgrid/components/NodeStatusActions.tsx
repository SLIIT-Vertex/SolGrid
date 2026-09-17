import { useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/common/Button'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { Dialog } from '@/components/common/Dialog'
import { useToast } from '@/components/common/useToast'
import {
  useActivateMicrogridNode,
  useDeactivateMicrogridNode,
} from '@/features/microgrid/hooks/useSetMicrogridNodeActive'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { MicrogridNode } from '@/features/microgrid/types'
import { getErrorMessage, isConflictError } from '@/lib/problemDetails'

type LifecycleIntent = 'activate' | 'deactivate'

export function NodeStatusActions({ node }: { node: MicrogridNode }) {
  const queryClient = useQueryClient()
  const { showToast } = useToast()
  const activateNode = useActivateMicrogridNode(node.id)
  const deactivateNode = useDeactivateMicrogridNode(node.id)
  const [intent, setIntent] = useState<LifecycleIntent | null>(null)
  const [conflictMessage, setConflictMessage] = useState<string | null>(null)
  const inFlight = useRef(false)

  const isDeactivating = node.status === 'Active'
  const isMutating = activateNode.isPending || deactivateNode.isPending

  const handleConfirm = async () => {
    if (!intent || isMutating || inFlight.current) return
    inFlight.current = true

    try {
      if (intent === 'deactivate') {
        await deactivateNode.mutateAsync()
        showToast(`${node.name} was deactivated.`)
      } else {
        await activateNode.mutateAsync()
        showToast(`${node.name} was activated.`)
      }
      setIntent(null)
    } catch (error) {
      if (intent === 'deactivate' && isConflictError(error)) {
        setIntent(null)
        setConflictMessage(
          getErrorMessage(error, 'A station with active reservations cannot be deactivated.'),
        )
        queryClient.invalidateQueries({ queryKey: microgridKeys.detail(node.id) })
        queryClient.invalidateQueries({ queryKey: microgridKeys.slots(node.id) })
        return
      }

      showToast(getErrorMessage(error, 'Could not change this Microgrid Node.'), 'error')
    } finally {
      inFlight.current = false
    }
  }

  return (
    <>
      {isDeactivating ? (
        <Button
          type="button"
          variant="danger"
          className="w-full sm:w-auto"
          disabled={isMutating}
          onClick={() => setIntent('deactivate')}
        >
          Deactivate Node
        </Button>
      ) : (
        <Button
          type="button"
          variant="secondary"
          className="w-full sm:w-auto"
          disabled={isMutating}
          onClick={() => setIntent('activate')}
        >
          Activate Node
        </Button>
      )}

      <ConfirmDialog
        open={intent !== null}
        title={intent === 'deactivate' ? 'Deactivate Microgrid Node?' : 'Activate Microgrid Node?'}
        description={
          intent === 'deactivate'
            ? `${node.name} will become unavailable for new operations. Existing reservation rules are checked by the server.`
            : `${node.name} will become available for new operations.`
        }
        confirmLabel={intent === 'deactivate' ? 'Deactivate Node' : 'Activate Node'}
        isDestructive={intent === 'deactivate'}
        isLoading={isMutating}
        onConfirm={handleConfirm}
        onCancel={() => {
          if (isMutating) return
          setIntent(null)
        }}
      />

      <Dialog
        open={conflictMessage !== null}
        onClose={() => setConflictMessage(null)}
        title={`Unable to deactivate ${node.name}`}
        description={conflictMessage ?? undefined}
        footer={
          <Button type="button" variant="secondary" onClick={() => setConflictMessage(null)}>
            Close
          </Button>
        }
      >
        <p className="text-sm text-ink-600">The node remains Active. Resolve the conflict, then try again.</p>
      </Dialog>
    </>
  )
}
