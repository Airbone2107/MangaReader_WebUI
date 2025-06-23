import { FormControl, FormHelperText, InputLabel, MenuItem, Select, TextField } from '@mui/material'
import React from 'react'
import { useController } from 'react-hook-form'

function FormInput({ name, control, label, type = 'text', options, ...props }) {
  const {
    field,
    fieldState: { error },
  } = useController({
    name,
    control,
  })

  if (type === 'select' && options) {
    const selectValue = field.value === null ? '' : field.value;

    return (
      <FormControl fullWidth margin="normal" error={!!error}>
        <InputLabel id={`${name}-label`}>{label}</InputLabel>
        <Select
          labelId={`${name}-label`}
          id={name}
          {...field}
          value={selectValue}
          label={label}
          {...props}
        >
          {(name === 'publicationDemographic' || name === 'year') && (
            <MenuItem value="">
              <em>Không chọn / Để trống</em>
            </MenuItem>
          )}
          {options.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </Select>
        {error && <FormHelperText>{error.message}</FormHelperText>}
      </FormControl>
    )
  }

  return (
    <TextField
      {...field}
      label={label}
      type={type}
      fullWidth
      margin="normal"
      error={!!error}
      helperText={error ? error.message : null}
      {...props}
    />
  )
}

export default FormInput 