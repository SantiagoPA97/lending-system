import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/components/domain/page-header'
import { Card, CardContent } from '@/components/ui/card'

export default function SearchPage() {
  const [params] = useSearchParams()
  const q = params.get('q') ?? ''
  return (
    <>
      <PageHeader
        eyebrow="Find"
        title="Search"
        description={q ? `Results for “${q}”` : 'Search across companies and facilities.'}
      />
      <Card>
        <CardContent className="py-10 text-center text-sm text-muted">
          Unified search is on its way.
        </CardContent>
      </Card>
    </>
  )
}
