import { ChoiceOption } from '../../models';

export interface PlayableQuestion {
  id: string;
  prompt: string;
  questionType: string;
  passageText?: string | null;
  optionA?: string | null;
  optionB?: string | null;
  optionC?: string | null;
  optionD?: string | null;
  options?: ChoiceOption[] | null;
  promptImageUrl?: string | null;
  children?: PlayableQuestion[] | null;
}

export interface AnswerImageDraft {
  mediaAssetId: string | null;
  imageUrl: string | null;
}
