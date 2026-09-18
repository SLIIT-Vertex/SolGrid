import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { useToast } from '@/components/common/useToast'
import { MicrogridBackLink } from '@/features/microgrid/components/MicrogridBackLink'
import { NodeForm } from '@/features/microgrid/components/NodeForm'
import { useMicrogridNode } from '@/features/microgrid/hooks/useMicrogridNode'
import { useUpdateMicrogridNode } from '@/features/microgrid/hooks/useUpdateMicrogridNode'
import type { UpdateSolarStationRequest } from '@/features/microgrid/types'
import { toNodeFormValues } from '@/features/microgrid/validation'
import { getErrorMessage, isNotFoundError } from '@/lib/problemDetails'

export function EditMicrogridNodePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const { data: node, error, isError, isLoading, refetch } = useMicrogridNode(id)
  const updateNode = useUpdateMicrogridNode(id ?? '')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const detailsPath = `/microgrid/${id}`

  const handleSubmit = async (request: UpdateSolarStationRequest) => {
    setSubmitError(null)
    try {
      const updated = await updateNode.mutateAsync(request)
      showToast(`${updated.name} was updated.`)
      navigate(`/microgrid/${updated.id}`)
    } catch (submitFailure) {
      setSubmitError(getErrorMessage(submitFailure, 'Could not update this Microgrid Node.'))
    }
  }

  return (
    <div className="mx-auto flex h-full w-full max-w-3xl flex-col">
      <MicrogridBackLink to={id ? detailsPath : '/microgrid'} label="Node details" />
      <div className="mb-6">
        <h2 className="text-lg font-semibold text-ink-900">Edit Microgrid Node</h2>
        <p className="mt-1 text-sm text-ink-500">
          Update identity, location, and generation capacity. Schedule and battery slots are managed on the node
          details page.
        </p>
      </div>

      {!id || isNotFoundError(error) ? (
        <EmptyState
          title="This Microgrid Node was not found."
          description="It may have been removed, or the link may be incorrect."
          action={
            <Button type="button" variant="secondary" onClick={() => navigate('/microgrid')}>
              Back to Microgrid Nodes
            </Button>
          }
        />
      ) : isLoading ? (
        <LoadingState label="Loading Microgrid Node…" />
      ) : isError || !node ? (
        <ErrorState message="We couldn't load this Microgrid Node." onRetry={() => refetch()} />
      ) : (
        <NodeForm
          key={node.id}
          mode="edit"
          defaultValues={toNodeFormValues(node)}
          isSubmitting={updateNode.isPending}
          submitError={submitError}
          onSubmit={handleSubmit}
          onCancel={() => navigate(detailsPath)}
        />
      )}
    </div>
  )
}
