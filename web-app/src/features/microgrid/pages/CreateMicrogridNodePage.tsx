import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useToast } from '@/components/common/useToast'
import { MicrogridBackLink } from '@/features/microgrid/components/MicrogridBackLink'
import { NodeForm } from '@/features/microgrid/components/NodeForm'
import { useCreateMicrogridNode } from '@/features/microgrid/hooks/useCreateMicrogridNode'
import type { CreateSolarStationRequest } from '@/features/microgrid/types'
import { getErrorMessage } from '@/lib/problemDetails'

export function CreateMicrogridNodePage() {
  const navigate = useNavigate()
  const { showToast } = useToast()
  const createNode = useCreateMicrogridNode()
  const [submitError, setSubmitError] = useState<string | null>(null)

  const handleSubmit = async (request: CreateSolarStationRequest) => {
    setSubmitError(null)
    try {
      const node = await createNode.mutateAsync(request)
      showToast(`${node.name} was created.`)
      navigate(`/microgrid/${node.id}`)
    } catch (error) {
      setSubmitError(getErrorMessage(error, 'Could not create this Microgrid Node.'))
    }
  }

  return (
    <div className="mx-auto flex h-full w-full max-w-3xl flex-col">
      <MicrogridBackLink />
      <div className="mb-6">
        <h2 className="text-lg font-semibold text-ink-900">Create Microgrid Node</h2>
        <p className="mt-1 text-sm text-ink-500">
          Capture identity, GPS location, generation capacity, weekly hours, and optional battery slots.
        </p>
      </div>

      <NodeForm
        isSubmitting={createNode.isPending}
        submitError={submitError}
        onSubmit={handleSubmit}
        onCancel={() => navigate('/microgrid')}
      />
    </div>
  )
}
