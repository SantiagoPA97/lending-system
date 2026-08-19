import type { UseFormReturn } from 'react-hook-form'
import { FieldError, Input, Label } from '@/components/ui/input'
import type { CompanyFormValues } from './company-form'

export function CompanyFormFields({
  form,
  idPrefix,
}: {
  form: UseFormReturn<CompanyFormValues>
  idPrefix: string
}) {
  const {
    register,
    formState: { errors },
  } = form
  const fieldId = (name: string) => `${idPrefix}-${name}`

  return (
    <div className="grid gap-4">
      <div>
        <Label htmlFor={fieldId('name')}>Name</Label>
        <Input
          id={fieldId('name')}
          placeholder="Andes Coffee Exporters"
          aria-invalid={!!errors.name}
          {...register('name')}
        />
        <FieldError message={errors.name?.message} />
      </div>
      <div>
        <Label htmlFor={fieldId('legalName')}>Legal name</Label>
        <Input
          id={fieldId('legalName')}
          placeholder="Andes Coffee Exporters S.A.S."
          aria-invalid={!!errors.legalName}
          {...register('legalName')}
        />
        <FieldError message={errors.legalName?.message} />
      </div>
      <div className="grid gap-4 sm:grid-cols-[1fr_7rem]">
        <div>
          <Label htmlFor={fieldId('registrationNumber')}>Registration number</Label>
          <Input
            id={fieldId('registrationNumber')}
            placeholder="900123456-7"
            className="font-mono"
            aria-invalid={!!errors.registrationNumber}
            {...register('registrationNumber')}
          />
          <FieldError message={errors.registrationNumber?.message} />
        </div>
        <div>
          <Label htmlFor={fieldId('country')}>Country</Label>
          <Input
            id={fieldId('country')}
            placeholder="CO"
            maxLength={2}
            className="uppercase"
            aria-invalid={!!errors.country}
            {...register('country')}
          />
          <FieldError message={errors.country?.message} />
        </div>
      </div>
      <div>
        <Label htmlFor={fieldId('industry')}>Industry</Label>
        <Input
          id={fieldId('industry')}
          placeholder="Agriculture"
          aria-invalid={!!errors.industry}
          {...register('industry')}
        />
        <FieldError message={errors.industry?.message} />
      </div>
      <div>
        <Label htmlFor={fieldId('contactEmail')}>Contact email</Label>
        <Input
          id={fieldId('contactEmail')}
          type="email"
          placeholder="finance@andescoffee.co"
          aria-invalid={!!errors.contactEmail}
          {...register('contactEmail')}
        />
        <FieldError message={errors.contactEmail?.message} />
      </div>
    </div>
  )
}
