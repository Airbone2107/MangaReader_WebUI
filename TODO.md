# TODO: Cải thiện giao diện Trang Tạo/Chỉnh sửa Manga

Tài liệu này mô tả các bước cần thực hiện để cải thiện giao diện người dùng của trang tạo và chỉnh sửa Manga trong ứng dụng MangaReader_ManagerUI.

## Mục tiêu

1.  **Đồng bộ Label:** Đảm bảo label của các trường "Tác giả / Họa sĩ" và "Tags" hiển thị nhất quán với các trường khác trong form (label nằm trong viền của input/dropdown).
2.  **Thiết kế lại Dropdown Tags:**
    *   Hiển thị danh sách các tag tùy chọn dưới dạng các khối chữ nhật (sử dụng `Chip` của MUI).
    *   Cho phép người dùng chọn nhiều tags mà không làm ẩn dropdown ngay sau mỗi lần chọn.
    *   Các tag đã chọn sẽ hiển thị dưới dạng Chip bên dưới ô input (hành vi mặc định của `Autocomplete multiple`).

## Các thay đổi cần thực hiện

Các thay đổi chủ yếu tập trung vào file `MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaForm.jsx`.

### Bước 1: Đồng bộ hóa Label cho trường "Tác giả / Họa sĩ" và "Tags"

#### 1.1. Vấn đề

Hiện tại, label "Tác giả / Họa sĩ" và "Tags" đang được hiển thị bằng component `Typography` phía trên các `Autocomplete` tương ứng, thay vì là label chuẩn của `TextField` bên trong `Autocomplete`. Điều này gây ra sự thiếu nhất quán so với các trường khác.

#### 1.2. Giải pháp

*   Bỏ các component `Typography` hiện tại cho "Tác giả / Họa sĩ" và "Tags".
*   Thêm prop `label` trực tiếp vào `TextField` được render bởi `Autocomplete` tương ứng.

#### 1.3. Cập nhật code trong `MangaForm.jsx`

```javascript
// MangaReader_ManagerUI\mangareader_managerui.client\src\features\manga\components\MangaForm.jsx
import { Add as AddIcon, CheckBox as CheckBoxIcon, CheckBoxOutlineBlank as CheckBoxOutlineBlankIcon, Delete as DeleteIcon } from '@mui/icons-material' // Thêm icon
import { Autocomplete, Box, Button, Checkbox, Chip, FormControlLabel, Grid, Paper, Switch, TextField, Typography } from '@mui/material' // Thêm Checkbox, Paper
import React, { useEffect, useState } from 'react'
import authorApi from '../../../api/authorApi'
import tagApi from '../../../api/tagApi'
import FormInput from '../../../components/common/FormInput'
import {
    CONTENT_RATING_OPTIONS,
    MANGA_STAFF_ROLE_OPTIONS,
    MANGA_STATUS_OPTIONS,
    ORIGINAL_LANGUAGE_OPTIONS,
    PUBLICATION_DEMOGRAPHIC_OPTIONS,
} from '../../../constants/appConstants'
import useFormWithZod from '../../../hooks/useFormWithZod'
import { createMangaSchema, updateMangaSchema } from '../../../schemas/mangaSchema'
import { handleApiError } from '../../../utils/errorUtils'

/**
 * @typedef {import('../../../types/manga').Manga} Manga
 * @typedef {import('../../../types/manga').Author} Author
 * @typedef {import('../../../types/manga').Tag} Tag
 * @typedef {import('../../../types/manga').MangaAuthorInput} MangaAuthorInput
 * @typedef {import('../../../types/manga').SelectedRelationship} SelectedRelationship
 */

/**
 * MangaForm component for creating or editing manga.
 * @param {object} props
 * @param {Manga} [props.initialData] - Initial data for editing.
 * @param {function(CreateMangaRequest | UpdateMangaRequest): void} props.onSubmit - Function to handle form submission.
 * @param {boolean} props.isEditMode - True if in edit mode, false for create mode.
 */
function MangaForm({ initialData, onSubmit, isEditMode }) {
  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
    // Lấy thêm `getValues` để truy cập giá trị form cho nút "Thêm" tác giả
    getValues,
  } = useFormWithZod({
    schema: isEditMode ? updateMangaSchema : createMangaSchema,
    defaultValues: initialData
      ? {
          title: initialData.attributes.title || '',
          originalLanguage: initialData.attributes.originalLanguage || '',
          publicationDemographic: initialData.attributes.publicationDemographic || null,
          status: initialData.attributes.status || 'Ongoing',
          year: initialData.attributes.year || null,
          contentRating: initialData.attributes.contentRating || 'Safe',
          isLocked: initialData.attributes.isLocked || false,
          tagIds: initialData.relationships
            ?.filter((rel) => rel.type === 'tag')
            .map((rel) => rel.id) || [],
          authors: initialData.relationships
            ?.filter((rel) => rel.type === 'author' || rel.type === 'artist')
            .map((rel) => ({
              authorId: rel.id,
              role: rel.type === 'author' ? 'Author' : 'Artist',
            })) || [],
          tempAuthor: null, // Thêm state tạm cho Autocomplete tác giả
          tempAuthorRole: 'Author', // Giữ nguyên state tạm cho vai trò
        }
      : {
          title: '',
          originalLanguage: 'ja',
          publicationDemographic: null,
          status: 'Ongoing',
          year: new Date().getFullYear(),
          contentRating: 'Safe',
          isLocked: false,
          tagIds: [],
          authors: [],
          tempAuthor: null,
          tempAuthorRole: 'Author',
        },
  })

  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedAuthorsVisual, setSelectedAuthorsVisual] = useState([])
  /** @type {[SelectedRelationship[], React.Dispatch<React.SetStateAction<SelectedRelationship[]>>]} */
  const [selectedTagsVisual, setSelectedTagsVisual] = useState([])

  const [availableAuthors, setAvailableAuthors] = useState([])
  const [availableTags, setAvailableTags] = useState([])

  const currentAuthorsFormValue = watch('authors') || []
  const currentTagIdsFormValue = watch('tagIds') || []
  const isLocked = watch('isLocked')

  // State cho Autocomplete chọn tác giả (không phải là một phần của form data chính thức)
  const [tempSelectedAuthor, setTempSelectedAuthor] = useState(null);


  useEffect(() => {
    const fetchDropdownData = async () => {
      try {
        const authorsResponse = await authorApi.getAuthors({ limit: 1000 })
        setAvailableAuthors(authorsResponse.data.map(a => ({ id: a.id, name: a.attributes.name })))

        const tagsResponse = await tagApi.getTags({ limit: 1000 })
        setAvailableTags(tagsResponse.data.map(t => ({ id: t.id, name: t.attributes.name, tagGroupId: t.attributes.tagGroupId, tagGroupName: t.attributes.tagGroupName })))
      } catch (error) {
        handleApiError(error, 'Không thể tải dữ liệu tác giả/tag.');
      }
    }
    fetchDropdownData()
  }, [])

  useEffect(() => {
    if (initialData && availableAuthors.length > 0 && availableTags.length > 0) {
      const initialAuthorRelationships = initialData.relationships
        ?.filter((rel) => rel.type === 'author' || rel.type === 'artist')
        .map((rel) => ({
          authorId: rel.id,
          role: rel.type === 'author' ? 'Author' : 'Artist',
        })) || [];

      const hydratedAuthors = initialAuthorRelationships
        .map((rel) => {
          const author = availableAuthors.find((a) => a.id === rel.authorId)
          return author ? { ...author, role: rel.role } : null
        })
        .filter(Boolean)

      setSelectedAuthorsVisual(hydratedAuthors)
      // setValue('authors', initialAuthorRelationships) // RHF đã có giá trị từ defaultValues

      const initialTagIds = initialData.relationships
        ?.filter((rel) => rel.type === 'tag')
        .map((rel) => rel.id) || [];

      const hydratedTags = initialTagIds
        .map((tagId) => {
          const tag = availableTags.find((t) => t.id === tagId)
          return tag ? { id: tag.id, name: tag.name, tagGroupName: tag.attributes?.tagGroupName || 'N/A' } : null // Lấy thêm tagGroupName
        })
        .filter(Boolean)

      setSelectedTagsVisual(hydratedTags)
      // setValue('tagIds', initialTagIds) // RHF đã có giá trị từ defaultValues
    }
  }, [initialData, availableAuthors, availableTags, setValue]) // `setValue` được giữ lại nếu bạn muốn cập nhật programmatically
  
  // Cập nhật selectedAuthorsVisual khi currentAuthorsFormValue thay đổi
  useEffect(() => {
    const hydratedAuthors = currentAuthorsFormValue
      .map((formAuthor) => {
        const authorDetails = availableAuthors.find(a => a.id === formAuthor.authorId);
        return authorDetails ? { ...authorDetails, role: formAuthor.role } : null;
      })
      .filter(Boolean);
    setSelectedAuthorsVisual(hydratedAuthors);
  }, [currentAuthorsFormValue, availableAuthors]);

  // Cập nhật selectedTagsVisual khi currentTagIdsFormValue thay đổi
  useEffect(() => {
    const hydratedTags = currentTagIdsFormValue
      .map((tagId) => {
        const tagDetails = availableTags.find(t => t.id === tagId);
        return tagDetails ? { id: tagDetails.id, name: tagDetails.name, tagGroupName: tagDetails.tagGroupName } : null;
      })
      .filter(Boolean);
    setSelectedTagsVisual(hydratedTags);
  }, [currentTagIdsFormValue, availableTags]);


  const handleAddAuthorToList = () => {
    const role = getValues('tempAuthorRole') || 'Author'; // Lấy vai trò từ form
    if (!tempSelectedAuthor || !role) return;

    const newAuthorEntry = { authorId: tempSelectedAuthor.id, role: role };
    if (!currentAuthorsFormValue.some(
      (a) => a.authorId === newAuthorEntry.authorId && a.role === newAuthorEntry.role
    )) {
      setValue('authors', [...currentAuthorsFormValue, newAuthorEntry]);
      // Không cần cập nhật selectedAuthorsVisual ở đây nữa, useEffect sẽ xử lý
      setTempSelectedAuthor(null); // Reset ô chọn tác giả
    } else {
      handleApiError(null, `${tempSelectedAuthor.name} với vai trò ${role} đã được thêm.`);
    }
  };


  const handleRemoveAuthorVisual = (authorIdToRemove, roleToRemove) => {
    const updatedAuthorsFormValue = currentAuthorsFormValue.filter(
      (a) => !(a.authorId === authorIdToRemove && a.role === roleToRemove)
    );
    setValue('authors', updatedAuthorsFormValue);
    // Không cần cập nhật selectedAuthorsVisual ở đây nữa, useEffect sẽ xử lý
  };

  // Không cần handleAddTag và handleRemoveTag riêng biệt nữa nếu dùng Autocomplete `onChange` đúng cách

  // Custom Paper component cho Autocomplete Tags
  const HorizontalTagPaper = (props) => {
    return (
      <Paper {...props} sx={{ ...props.sx, maxHeight: 300, overflow: 'auto' }}>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', p: 1, gap: 0.5 }}>
          {props.children}
        </Box>
      </Paper>
    );
  };


  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ mt: 1 }}>
      <Grid container spacing={2} columns={{ xs: 4, sm: 6, md: 12 }}>
        <Grid item xs={4} sm={6} md={12}>
          <FormInput control={control} name="title" label="Tiêu đề Manga" />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="originalLanguage"
            label="Ngôn ngữ gốc"
            type="select"
            options={ORIGINAL_LANGUAGE_OPTIONS}
          />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="publicationDemographic"
            label="Đối tượng xuất bản"
            type="select"
            options={PUBLICATION_DEMOGRAPHIC_OPTIONS}
          />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="status"
            label="Trạng thái"
            type="select"
            options={MANGA_STATUS_OPTIONS}
          />
        </Grid>
        <Grid item xs={4} sm={3} md={6}>
          <FormInput
            control={control}
            name="year"
            label="Năm xuất bản"
            type="number"
            inputProps={{ min: 1000, max: new Date().getFullYear() + 5, step: 1 }} // Cho phép năm tương lai gần
          />
        </Grid>
        <Grid item xs={4} sm={6} md={12}>
          <FormInput
            control={control}
            name="contentRating"
            label="Đánh giá nội dung"
            type="select"
            options={CONTENT_RATING_OPTIONS}
          />
        </Grid>

        {/* Authors Section - Thay đổi Typography thành label */}
        <Grid item xs={12}>
          {/* Bỏ Typography "Tác giả / Họa sĩ" */}
          <Grid container spacing={1} alignItems="flex-end" columns={{ xs: 12, sm: 12, md: 12 }}>
            <Grid item xs={12} sm={7} md={7}>
              <Autocomplete
                options={availableAuthors}
                getOptionLabel={(option) => option.name}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                value={tempSelectedAuthor} // Sử dụng state tạm
                onChange={(event, newValue) => {
                  setTempSelectedAuthor(newValue); // Cập nhật state tạm
                }}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Tác giả / Họa sĩ" // <- THAY ĐỔI Ở ĐÂY
                    variant="outlined"
                    margin="normal"
                    error={!!errors.authors && currentAuthorsFormValue.length === 0} // Chỉ báo lỗi nếu chưa có tác giả nào được thêm
                    helperText={errors.authors && currentAuthorsFormValue.length === 0 ? "Vui lòng thêm ít nhất một tác giả/họa sĩ" : null}
                  />
                )}
              />
            </Grid>
            <Grid item xs={12} sm={3} md={3}>
              <FormInput
                control={control}
                name="tempAuthorRole" // Giữ nguyên, dùng để lấy giá trị cho nút "Thêm"
                label="Vai trò"
                type="select"
                options={MANGA_STAFF_ROLE_OPTIONS}
                defaultValue="Author"
                margin="normal"
              />
            </Grid>
            <Grid item xs={12} sm={2} md={2} sx={{ alignSelf: 'center', mt: { xs: 1, sm: '24px' } }}>
              <Button
                variant="contained"
                color="primary"
                onClick={handleAddAuthorToList}
                startIcon={<AddIcon />}
                fullWidth
              >
                Thêm
              </Button>
            </Grid>
          </Grid>
          <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {selectedAuthorsVisual.map((author, index) => (
              <Chip
                key={`${author.id}-${author.role}-${index}`}
                label={`${author.name} (${author.role})`}
                onDelete={() => handleRemoveAuthorVisual(author.id, author.role)}
                deleteIcon={<DeleteIcon />}
                color="primary"
                variant="outlined"
              />
            ))}
          </Box>
        </Grid>

        {/* Tags Section - Thay đổi Typography thành label, cập nhật Autocomplete */}
        <Grid item xs={12}>
          {/* Bỏ Typography "Tags" */}
          <Autocomplete
            multiple
            disableCloseOnSelect // <- THÊM PROP NÀY
            id="manga-tags-autocomplete"
            options={availableTags.sort((a, b) => a.tagGroupName.localeCompare(b.tagGroupName) || a.name.localeCompare(b.name))} // Sắp xếp theo nhóm rồi theo tên
            groupBy={(option) => option.tagGroupName} // Nhóm theo tagGroupName
            getOptionLabel={(option) => option.name}
            value={selectedTagsVisual} // Sử dụng state trực quan
            onChange={(event, newValue) => {
              // newValue là mảng các tag object đầy đủ {id, name, tagGroupName}
              setSelectedTagsVisual(newValue); // Cập nhật state trực quan
              setValue('tagIds', newValue.map(tag => tag.id)); // Cập nhật giá trị cho RHF
            }}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            renderOption={(props, option, { selected }) => (
              // props bao gồm key và các thuộc tính cần thiết cho <li>
              // Thêm sx để đảm bảo Chip chiếm toàn bộ chiều rộng của li và cách đều
              <Box component="li" {...props} sx={{ width: '100%', justifyContent: 'flex-start', px: 1, py: 0.5 }}>
                <Checkbox
                  icon={<CheckBoxOutlineBlankIcon fontSize="small" />}
                  checkedIcon={<CheckBoxIcon fontSize="small" />}
                  style={{ marginRight: 8 }}
                  checked={selected}
                />
                {/* Sử dụng Chip để hiển thị từng tag trong danh sách */}
                <Chip 
                  label={option.name} 
                  size="small" 
                  variant="outlined" 
                  sx={{ cursor: 'pointer', flexGrow: 1, justifyContent: 'flex-start' }} 
                /> 
              </Box>
            )}
            PaperComponent={HorizontalTagPaper} // Sử dụng PaperComponent tùy chỉnh
            renderInput={(params) => (
              <TextField
                {...params}
                variant="outlined"
                label="Tags" // <- THAY ĐỔI Ở ĐÂY
                placeholder="Chọn tags"
                margin="normal"
                error={!!errors.tagIds}
                helperText={errors.tagIds ? errors.tagIds.message : null}
              />
            )}
            // renderTags được giữ nguyên để hiển thị các chip đã chọn bên ngoài dropdown
            renderTags={(value, getTagProps) =>
              value.map((option, index) => (
                <Chip
                  key={option.id}
                  label={option.name}
                  {...getTagProps({ index })}
                  color="secondary" // Có thể đổi màu cho dễ phân biệt
                  variant="outlined"
                />
              ))
            }
            fullWidth
          />
        </Grid>
        
        {isEditMode && (
          <Grid item xs={4} sm={6} md={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={isLocked}
                  onChange={(e) => setValue('isLocked', e.target.checked)}
                  name="isLocked"
                  color="primary"
                />
              }
              label="Khóa Manga (Không cho phép đọc)"
              sx={{ mt: 2 }}
            />
          </Grid>
        )}

        <Grid item xs={4} sm={6} md={12}>
          <Button type="submit" variant="contained" color="primary" sx={{ mt: 3, mb: 2 }}>
            {isEditMode ? 'Cập nhật Manga' : 'Tạo Manga'}
          </Button>
        </Grid>
      </Grid>
    </Box>
  )
}

export default MangaForm
```

### Bước 2: Thiết kế lại Dropdown Tags

#### 2.1. Vấn đề
Dropdown chọn Tags hiện tại dùng `Autocomplete` cơ bản. Cần tùy chỉnh để:
*   Các tag trong danh sách lựa chọn (dropdown) hiển thị dưới dạng khối chữ nhật, có thể kèm checkbox.
*   Danh sách các tag options trong dropdown có thể hiển thị theo chiều ngang nếu không gian cho phép, và scroll được.
*   Người dùng có thể chọn nhiều tags mà dropdown không tự động đóng lại sau mỗi lựa chọn.

#### 2.2. Giải pháp
*   Sử dụng prop `disableCloseOnSelect` cho `Autocomplete` của Tags.
*   Tùy chỉnh `renderOption` để hiển thị mỗi tag với `Checkbox` và `Chip` (hoặc `Box` được style).
*   Tạo một `PaperComponent` tùy chỉnh cho `Autocomplete` để cho phép các options hiển thị theo chiều ngang và có thể cuộn.
*   Sắp xếp và nhóm các tags theo `tagGroupName` để dễ quản lý hơn.

#### 2.3. Cập nhật code trong `MangaForm.jsx`

Xem lại khối code ở **Bước 1.3** phía trên. Các thay đổi liên quan đến Tags đã được tích hợp:
*   Import `CheckBoxIcon`, `CheckBoxOutlineBlankIcon`, `Checkbox`, `Paper`.
*   `Autocomplete` cho Tags:
    *   Đã thêm `disableCloseOnSelect`.
    *   Đã thêm `groupBy={(option) => option.tagGroupName}` để nhóm các tag.
    *   `options` được sắp xếp: `availableTags.sort((a, b) => a.tagGroupName.localeCompare(b.tagGroupName) || a.name.localeCompare(b.name))`.
    *   `renderOption` được tùy chỉnh để hiển thị `Checkbox` và `Chip` cho mỗi tag.
    *   `PaperComponent={HorizontalTagPaper}` được sử dụng với `HorizontalTagPaper` là một component tùy chỉnh để style cho dropdown.
*   `selectedTagsVisual` được sử dụng cho prop `value` của `Autocomplete` tags.
*   Logic `onChange` của `Autocomplete` tags được cập nhật để làm việc với `selectedTagsVisual` (mảng object) và `setValue` cho `tagIds` (mảng string ID).