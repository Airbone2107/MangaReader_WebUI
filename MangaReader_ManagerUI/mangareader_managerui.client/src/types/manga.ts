import { RelationshipObject, ResourceObject } from './api'

// DTO mới cho Tag khi được nhúng trong Manga Attributes
export interface TagInMangaAttributesDto {
  name: string;
  tagGroupName: string;
}

// Attributes DTOs
export interface MangaAttributes {
  title: string
  originalLanguage: string
  publicationDemographic: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number | null
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  isLocked: boolean
  createdAt: string
  updatedAt: string
  tags: ResourceObject<TagInMangaAttributesDto>[]
}

export interface AuthorAttributes {
  name: string
  biography?: string
  createdAt: string
  updatedAt: string
}

export interface TagAttributes {
  name: string
  tagGroupName: string
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

// Request DTOs and Params
export type PublicationDemographicType = 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None';
export type MangaStatusType = 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled';
export type ContentRatingType = 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic';

export interface GetMangasParams {
  offset?: number;
  limit?: number;
  titleFilter?: string;
  statusFilter?: MangaStatusType | '';
  contentRatingFilter?: ContentRatingType | '';
  publicationDemographicsFilter?: PublicationDemographicType[];
  originalLanguageFilter?: string;
  yearFilter?: number | null;
  authorIdsFilter?: string[];
  includedTags?: string[];
  includedTagsMode?: 'AND' | 'OR';
  excludedTags?: string[];
  excludedTagsMode?: 'AND' | 'OR';
  orderBy?: string;
  ascending?: boolean;
  includes?: ('cover_art' | 'author' | 'artist')[];
}

export interface CreateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number | null
  contentRating: 'Safe' | 'Suggestive' | 'Erotica' | 'Pornographic'
  tagIds?: string[] // Array of GUID strings
  authors?: MangaAuthorInput[]
}

export interface UpdateMangaRequest {
  title: string
  originalLanguage: string
  publicationDemographic?: 'Shounen' | 'Shoujo' | 'Josei' | 'Seinen' | 'None' | null
  status: 'Ongoing' | 'Completed' | 'Hiatus' | 'Cancelled'
  year?: number | null
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
  uploadedByUserId: number 
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
  file: File; 
  volume?: string;
  description?: string;
}

export interface SelectedRelationship {
  id: string;
  name: string; 
  role?: 'Author' | 'Artist'; 
} 