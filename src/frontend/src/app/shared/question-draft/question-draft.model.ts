import { BankQuestionType } from '../../models';

export type AssessmentQuestionType = BankQuestionType | 'ShortAnswer' | 'MultipleChoice';

export interface QuestionOptionDraft {
  text: string;
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
}

export const BANK_QUESTION_TYPES: BankQuestionType[] = [
  'Choose',
  'TrueFalse',
  'SingleChoice',
  'MultiChoice',
  'Paragraph',
  'Underline',
  'FreeText'
];

export const CHILD_QUESTION_TYPES: AssessmentQuestionType[] = [
  'Choose',
  'TrueFalse',
  'SingleChoice',
  'MultiChoice',
  'FreeText'
];
