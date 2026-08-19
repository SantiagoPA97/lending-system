import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { Dialog, DialogBody, DialogFooter, DialogHeader } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { useCreateCompany } from '@/lib/queries'
import {
  apiErrorMessage,
  applyFieldErrors,
  companySchema,
  emptyCompanyValues,
  toCompanyRequest,
  type CompanyFormValues,
} from './company-form'
import { CompanyFormFields } from './company-form-fields'

export function CompanyCreateDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const navigate = useNavigate()
  const createCompany = useCreateCompany()
  const form = useForm<CompanyFormValues>({
    resolver: zodResolver(companySchema),
    defaultValues: emptyCompanyValues,
  })

  useEffect(() => {
    if (open) form.reset(emptyCompanyValues)
  }, [open, form])

  const onSubmit = form.handleSubmit((values) => {
    createCompany.mutate(toCompanyRequest(values), {
      onSuccess: (company) => {
        toast.success(`${company.name} added to the register`)
        onClose()
        navigate(`/companies/${company.id}`)
      },
      onError: (err) => {
        if (!applyFieldErrors(err, form.setError)) toast.error(apiErrorMessage(err))
      },
    })
  })

  return (
    <Dialog open={open} onClose={onClose}>
      <DialogHeader title="New company" onClose={onClose}>
        <p className="mt-0.5 text-[13px] text-muted">
          Register a borrower before opening facilities for it.
        </p>
      </DialogHeader>
      <form onSubmit={onSubmit} noValidate>
        <DialogBody>
          <CompanyFormFields form={form} idPrefix="new-company" />
        </DialogBody>
        <DialogFooter>
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createCompany.isPending}>
            Create company
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  )
}
