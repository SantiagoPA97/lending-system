import { useParams } from 'react-router-dom'
import { PageHeader } from '@/components/domain/page-header'
import { Card, CardContent } from '@/components/ui/card'

export default function CompanyDetail() {
  const { id } = useParams<{ id: string }>()
  return (
    <>
      <PageHeader eyebrow="Borrower" title="Company" description={`Company ${id}`} />
      <Card>
        <CardContent className="py-10 text-center text-sm text-muted">
          Company detail is on its way.
        </CardContent>
      </Card>
    </>
  )
}
