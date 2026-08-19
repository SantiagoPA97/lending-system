import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { formatDateTime } from '@/lib/format'
import { useUpdateCompany } from '@/lib/queries'
import type { CompanyDetailResponse } from '@/types/api'
import {
  apiErrorMessage,
  applyFieldErrors,
  companySchema,
  companyToValues,
  toCompanyRequest,
  type CompanyFormValues,
} from './company-form'
import { CompanyFormFields } from './company-form-fields'

export function CompanyDetailsCard({ company }: { company: CompanyDetailResponse }) {
  const updateCompany = useUpdateCompany(company.id)
  const form = useForm<CompanyFormValues>({
    resolver: zodResolver(companySchema),
    values: companyToValues(company),
  })

  const onSubmit = form.handleSubmit((values) => {
    updateCompany.mutate(toCompanyRequest(values), {
      onSuccess: () => toast.success('Changes saved'),
      onError: (err) => {
        if (!applyFieldErrors(err, form.setError)) toast.error(apiErrorMessage(err))
      },
    })
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Details</CardTitle>
      </CardHeader>
      <form onSubmit={onSubmit} noValidate>
        <CardContent>
          <CompanyFormFields form={form} idPrefix="company" />
        </CardContent>
        <div className="flex items-center justify-between gap-3 border-t border-line px-5 py-3">
          <p className="text-xs text-faint">Updated {formatDateTime(company.updatedAtUtc)}</p>
          <Button
            type="submit"
            size="sm"
            disabled={!form.formState.isDirty}
            loading={updateCompany.isPending}
          >
            Save changes
          </Button>
        </div>
      </form>
    </Card>
  )
}
