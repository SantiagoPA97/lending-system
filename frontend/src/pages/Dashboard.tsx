import { PageHeader } from '@/components/domain/page-header'
import { Card, CardContent } from '@/components/ui/card'

export default function Dashboard() {
  return (
    <>
      <PageHeader
        eyebrow="Portfolio"
        title="Dashboard"
        description="Committed and outstanding exposure across the book."
      />
      <Card>
        <CardContent className="py-10 text-center text-sm text-muted">
          Portfolio metrics are on their way.
        </CardContent>
      </Card>
    </>
  )
}
