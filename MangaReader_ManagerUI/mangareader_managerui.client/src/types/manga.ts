import { RelationshipObject } from './api'

// Attributes DTOs
export interface MangaAttributes {
  title: string
  originalLanguage: string
  publicationDemographic: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None'
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  createdAt: string
  updatedAt: string
}

export interface AuthorAttributes {
  name: string
  biography?: string
  createdAt: string
  updatedAt: string
}

export interface TagAttributes {
  name: string
  tagGroupId: string // GUID of the tag group
  tagGroupName: string // Name of the tag group
  createdAt: string
  updatedAt: string
}

export interface TagGroupAttributes {
  name: string
  createdAt: string
  updatedAt: string
}

export interface TranslatedMangaAttributes {
  languageKey: string
  title: string
  description?: string
  createdAt: string
  updatedAt: string
}

export interface ChapterAttributes {
  volume?: string
  chapterNumber?: string
  title?: string
  pagesCount: number
  publishAt: string
  readableAt: string
  createdAt: string
  updatedAt: string
}

export interface ChapterPageAttributes {
  pageNumber: number
  publicId: string
}

export interface CoverArtAttributes {
  volume?: string
  publicId: string
  description?: string
  createdAt: string
  updatedAt: string
}

// Full Resource Objects (including ID, type, attributes, relationships)
export interface Manga {
  id: string
  type: 'manga'
  attributes: MangaAttributes
  relationships?: RelationshipObject[]
  coverArtPublicId?: string
}

export interface Author {
  id: string
  type: 'author'
  attributes: AuthorAttributes
  relationships?: RelationshipObject[]
}

export interface Tag {
  id: string
  type: 'tag'
  attributes: TagAttributes
  relationships?: RelationshipObject[]
}

export interface TagGroup {
  id: string
  type: 'tag_group'
  attributes: TagGroupAttributes
  relationships?: RelationshipObject[]
}

export interface TranslatedManga {
  id: string
  type: 'translated_manga'
  attributes: TranslatedMangaAttributes
  relationships?: RelationshipObject[]
}

export interface Chapter {
  id: string
  type: 'chapter'
  attributes: ChapterAttributes
  relationships?: RelationshipObject[]
}

export interface ChapterPage {
  id: string
  type: 'chapter_page'
  attributes: ChapterPageAttributes
  relationships?: RelationshipObject[]
}

export interface CoverArt {
  id: string
  type: 'cover_art'
  attributes: CoverArtAttributes
  relationships?: RelationshipObject[]
}

// Request DTOs
export interface CreateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None'
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  tagIds?: string[] // Array of GUID strings
  authors?: MangaAuthorInput[]
}

export interface UpdateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None'
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  tagIds?: string[]
  authors?: MangaAuthorInput[]
}

export interface MangaAuthorInput {
  authorId: string // GUID string
  role: 'Author' | 'Artist'
}

export interface CreateAuthorRequest {
  name: string
  biography?: string
}

export interface UpdateAuthorRequest {
  name: string
  biography?: string
}

export interface CreateTagRequest {
  name: string
  tagGroupId: string
}

export interface UpdateTagRequest {
  name: string
  tagGroupId: string
}

export interface CreateTagGroupRequest {
  name: string
}

export interface UpdateTagGroupRequest {
  name: string
}

export interface CreateTranslatedMangaRequest {
  mangaId: string
  languageKey: string
  title: string
  description?: string
}

export interface UpdateTranslatedMangaRequest {
  languageKey: string
  title: string
  description?: string
}

export interface CreateChapterRequest {
  translatedMangaId: string
  uploadedByUserId: number // Assuming number for User ID for now
  volume?: string
  chapterNumber?: string
  title?: string
  publishAt: string
  readableAt: string
}

export interface UpdateChapterRequest {
  volume?: string
  chapterNumber?: string
  title?: string
  publishAt: string
  readableAt: string
}

export interface CreateChapterPageEntryRequest {
  pageNumber: number
}

export interface UpdateChapterPageDetailsRequest {
  pageNumber: number
}

export interface UploadCoverArtRequest {
  file: File; // For frontend forms
  volume?: string;
  description?: string;
}

// For use in forms where relationships are selected
export interface SelectedRelationship {
  id: string;
  name: string; // The display name of the related entity
  role?: 'Author' | 'Artist'; // Added for author selection in MangaForm
} 