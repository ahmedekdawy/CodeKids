import { BankQuestionType, MapMarker } from '../../models';

export type AssessmentQuestionType = BankQuestionType | 'ShortAnswer' | 'MultipleChoice';

export interface QuestionOptionDraft {
  text: string;
}

export interface MapMarkerDraft extends MapMarker {
  correctAnswer: string;
}

export interface QuestionDraft {
  id?: string;
  prompt: string;
  questionType: AssessmentQuestionType;
  passageText: string;
  options: QuestionOptionDraft[];
  correctAnswer: string;
  correctKeys: string[];
  points: number;
  children: QuestionDraft[];
  promptImageMediaAssetId?: string | null;
  promptImageUrl?: string | null;
  mapMarkers: MapMarkerDraft[];
}

export const BANK_QUESTION_TYPES: BankQuestionType[] = [
  'Choose',
  'TrueFalse',
  'SingleChoice',
  'MultiChoice',
  'Order',
  'Map',
  'Paragraph',
  'Underline',
  'FreeText',
  'ShortAnswer'
];

export const CHILD_QUESTION_TYPES: AssessmentQuestionType[] = [
  'Choose',
  'TrueFalse',
  'SingleChoice',
  'MultiChoice',
  'Order',
  'FreeText',
  'ShortAnswer'
];
