import { useParams } from 'react-router-dom'
import { PageHeader } from '@/components/domain/page-header'
import { Card, CardContent } from '@/components/ui/card'

export default function FacilityDetail() {
  const { id } = useParams<{ id: string }>()
  return (
    <>
      <PageHeader eyebrow="Credit" title="Facility" description={`Facility ${id}`} />
      <Card>
        <CardContent className="py-10 text-center text-sm text-muted">
          Facility detail is on its way.
        </CardContent>
      </Card>
    </>
  )
}
