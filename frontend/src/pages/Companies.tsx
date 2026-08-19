import { PageHeader } from '@/components/domain/page-header'
import { Card, CardContent } from '@/components/ui/card'

export default function Companies() {
  return (
    <>
      <PageHeader
        eyebrow="Borrowers"
        title="Companies"
        description="Every borrower on the book, with their standing and facilities."
      />
      <Card>
        <CardContent className="py-10 text-center text-sm text-muted">
          The company register is on its way.
        </CardContent>
      </Card>
    </>
  )
}
