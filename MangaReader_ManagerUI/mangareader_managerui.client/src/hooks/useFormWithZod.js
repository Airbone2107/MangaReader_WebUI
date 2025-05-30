import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'

/**
 * Custom hook for React Hook Form with Zod resolver.
 * @template TSchema
 * @param {object} options
 * @param {import('zod').ZodSchema<TSchema>} options.schema - Zod validation schema.
 * @param {TSchema} [options.defaultValues] - Default form values.
 * @param {import('react-hook-form').Mode} [options.mode='onChange'] - Validation mode.
 * @returns {import('react-hook-form').UseFormReturn<TSchema>}
 */
function useFormWithZod({ schema, defaultValues, mode = 'onChange' }) {
  const form = useForm({
    resolver: zodResolver(schema),
    defaultValues: defaultValues,
    mode: mode,
  })

  return form
}

export default useFormWithZod 