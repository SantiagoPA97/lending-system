import { PageHeader } from '@/components/domain/page-header'
import { Card, CardContent } from '@/components/ui/card'

export default function Facilities() {
  return (
    <>
      <PageHeader
        eyebrow="Credit"
        title="Facilities"
        description="Commitments, terms and outstanding balances across all borrowers."
      />
      <Card>
        <CardContent className="py-10 text-center text-sm text-muted">
          The facility register is on its way.
        </CardContent>
      </Card>
    </>
  )
}
