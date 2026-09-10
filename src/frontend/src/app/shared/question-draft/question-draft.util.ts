import { AssignmentQuestion, BankQuestionType, ChoiceOption, TeacherQuizQuestionDetail } from '../../models';
import {
  AssessmentQuestionType,
  BANK_QUESTION_TYPES,
  CHILD_QUESTION_TYPES,
  QuestionDraft,
  QuestionOptionDraft
} from './question-draft.model';

export function emptyQuestionDraft(type: AssessmentQuestionType = 'SingleChoice'): QuestionDraft {
  const normalized = type === 'MultipleChoice' ? 'SingleChoice' : type;
  return {
    prompt: '',
    questionType: normalized,
    passageText: '',
    options: [{ text: '' }, { text: '' }],
    correctAnswer: normalized === 'TrueFalse' ? 'True' : '',
    correctKeys: [],
    points: 1,
    children: normalized === 'Paragraph' ? [emptyQuestionDraft('SingleChoice')] : [],
    mapMarkers: []
  };
}

export function isParagraph(type: string): boolean {
  return type === 'Paragraph';
}

export function isShortAnswer(type: string): boolean {
  return type === 'ShortAnswer';
}

export function isFreeText(type: string): boolean {
  return type === 'FreeText';
}

export function isTeacherGradedText(type: string): boolean {
  return isShortAnswer(type) || isFreeText(type);
}

export function needsOptions(type: string): boolean {
  return (
    type === 'Choose' ||
    type === 'SingleChoice' ||
    type === 'MultiChoice' ||
    type === 'MultipleChoice' ||
    type === 'Order'
  );
}

export function isMulti(type: string): boolean {
  return type === 'MultiChoice';
}

export function isOrder(type: string): boolean {
  return type === 'Order';
}

export function isMap(type: string): boolean {
  return type === 'Map';
}

export function isComplete(type: string): boolean {
  return type === 'Complete';
}

export type CompletePart =
  | { kind: 'text'; text: string }
  | { kind: 'blank'; index: number; expected: string };

export function parseCompleteParts(text: string | null | undefined): CompletePart[] {
  const source = text || '';
  const parts: CompletePart[] = [];
  const re = /\{\{blank\}\}|##([\s\S]*?)##/g;
  let last = 0;
  let index = 0;
  let match: RegExpExecArray | null;
  while ((match = re.exec(source))) {
    if (match.index > last) {
      parts.push({ kind: 'text', text: source.slice(last, match.index) });
    }
    parts.push({ kind: 'blank', index, expected: (match[1] || '').trim() });
    index += 1;
    last = match.index + match[0].length;
  }
  if (last < source.length || parts.length === 0) {
    if (last < source.length) parts.push({ kind: 'text', text: source.slice(last) });
  }
  return parts;
}

export function extractCompleteAnswers(text: string | null | undefined): string[] {
  return parseCompleteParts(text)
    .filter((part): part is Extract<CompletePart, { kind: 'blank' }> => part.kind === 'blank')
    .map((part) => part.expected)
    .filter((value) => value.length > 0);
}

export function encodeCompleteAnswers(values: string[]): string {
  return JSON.stringify(values.map((value) => (value || '').trim()));
}

export function parseCompleteAnswers(value: string | null | undefined): string[] {
  if (!value?.trim()) return [];
  try {
    const parsed = JSON.parse(value) as unknown;
    if (Array.isArray(parsed)) {
      return parsed.map((item) => String(item ?? '').trim());
    }
  } catch {
    return extractCompleteAnswers(value);
  }
  return extractCompleteAnswers(value);
}

export function encodeMapAnswers(markers: { id: string; correctAnswer?: string }[]): string {
  const answers: Record<string, string> = {};
  for (const marker of markers) {
    answers[marker.id] = (marker.correctAnswer || '').trim();
  }
  return JSON.stringify(answers);
}

export function parseMapAnswers(value: string | null | undefined): Record<string, string> {
  if (!value?.trim()) return {};
  try {
    const parsed = JSON.parse(value) as Record<string, unknown>;
    const result: Record<string, string> = {};
    for (const [key, answer] of Object.entries(parsed || {})) {
      result[key] = String(answer ?? '').trim();
    }
    return result;
  } catch {
    return {};
  }
}

export function optionLabel(index: number): string {
  return String.fromCharCode(65 + index);
}

export function formatMapAnswer(value: string | null | undefined): string {
  const answers = parseMapAnswers(value);
  const entries = Object.entries(answers);
  if (!entries.length) return (value || '').trim();
  return entries.map(([id, answer]) => `${id}: ${answer}`).join(', ');
}

/** Show stored keys (A / A,C) together with the option text for teacher review. */
export function formatChoiceAnswer(
  value: string | null | undefined,
  options?: ChoiceOption[] | null
): string {
  const raw = (value || '').trim();
  if (!raw) return '';
  if (raw.startsWith('[')) {
    const complete = parseCompleteAnswers(raw);
    if (complete.length) return complete.join(', ');
  }
  if (raw.startsWith('{')) {
    const mapFormatted = formatMapAnswer(raw);
    if (mapFormatted) return mapFormatted;
  }
  if (!options?.length) return raw;
  return raw
    .split(',')
    .map((part) => part.trim())
    .filter(Boolean)
    .map((key) => {
      const match = options.find((option) => option.key.toUpperCase() === key.toUpperCase());
      return match ? `${match.key}) ${match.text}` : key;
    })
    .join(', ');
}

export function choiceKeySelected(value: string | null | undefined, key: string): boolean {
  const raw = (value || '').trim();
  if (!raw) return false;
  return raw
    .split(',')
    .map((part) => part.trim().toUpperCase())
    .includes(key.toUpperCase());
}

export function filledOptions(list: QuestionOptionDraft[]): { key: string; text: string }[] {
  return list
    .map((option, index) => ({ key: optionLabel(index), text: (option.text || '').trim() }))
    .filter((option) => option.text.length > 0);
}

export function questionTypeLabelKey(type: string): string {
  const map: Record<string, string> = {
    ShortAnswer: 'assignType.shortAnswer',
    MultipleChoice: 'assignType.multipleChoice',
    Choose: 'qtype.choose',
    TrueFalse: 'qtype.trueFalse',
    SingleChoice: 'qtype.singleChoice',
    MultiChoice: 'qtype.multiChoice',
    Order: 'qtype.order',
    Map: 'qtype.map',
    Paragraph: 'qtype.paragraph',
    Underline: 'qtype.underline',
    Complete: 'qtype.complete',
    FreeText: 'qtype.freeText'
  };
  return map[type] ?? type;
}

export function editorTypes(
  allowShortAnswer: boolean,
  allowFreeText = true
): AssessmentQuestionType[] {
  let bank = [...BANK_QUESTION_TYPES];
  if (!allowFreeText) {
    bank = bank.filter((type) => type !== 'FreeText');
  }
  // ShortAnswer is already in BANK_QUESTION_TYPES; the legacy flag still prepends it for
  // assignment editors that used AssessmentQuestionType before it lived on the bank enum.
  if (allowShortAnswer && !bank.includes('ShortAnswer')) {
    return ['ShortAnswer', ...bank];
  }
  if (!allowShortAnswer) {
    bank = bank.filter((type) => type !== 'ShortAnswer');
  }
  return bank;
}

export function childQuestionTypes(allowShortAnswer: boolean): AssessmentQuestionType[] {
  const types = [...CHILD_QUESTION_TYPES];
  if (!allowShortAnswer) {
    return types.filter((type) => type !== 'ShortAnswer');
  }
  return types;
}

export function normalizeType(type: string | null | undefined): AssessmentQuestionType {
  if (type === 'MultipleChoice') return 'SingleChoice';
  if (type === 'ShortAnswer') return 'ShortAnswer';
  if ((BANK_QUESTION_TYPES as string[]).includes(type || '')) {
    return type as BankQuestionType;
  }
  return 'SingleChoice';
}

export function applyTypeDefaults(draft: QuestionDraft): void {
  if (isParagraph(draft.questionType) && draft.children.length === 0) {
    draft.children.push(emptyQuestionDraft('SingleChoice'));
  }
  if (!isParagraph(draft.questionType)) {
    draft.children = [];
  }
  if (draft.questionType === 'TrueFalse') {
    draft.correctAnswer = draft.correctAnswer === 'False' ? 'False' : 'True';
    draft.correctKeys = [];
  } else if (needsOptions(draft.questionType)) {
    if (draft.options.length < 2) draft.options = [{ text: '' }, { text: '' }];
    if (isOrder(draft.questionType)) {
      draft.correctKeys = [];
      draft.correctAnswer = filledOptions(draft.options)
        .map((option) => option.key)
        .join(',');
    }
  } else if (isMap(draft.questionType)) {
    draft.options = [];
    draft.correctKeys = [];
    if (!draft.mapMarkers) draft.mapMarkers = [];
    draft.correctAnswer = encodeMapAnswers(draft.mapMarkers);
  } else if (isComplete(draft.questionType)) {
    draft.options = [];
    draft.correctKeys = [];
    draft.correctAnswer = encodeCompleteAnswers(extractCompleteAnswers(draft.passageText));
  } else {
    draft.correctKeys = [];
  }
}

export function validateQuestionDraft(draft: QuestionDraft, index = 1): string | null {
  const prompt = plainPrompt(draft.prompt);
  if (!prompt && !isParagraph(draft.questionType)) {
    return 'teacher.qbank.required';
  }
  if (isParagraph(draft.questionType)) {
    if (!(draft.passageText || '').trim()) {
      return 'teacher.qbank.paragraphText';
    }
    if (!draft.children.length) {
      return 'teacher.qbank.childQuestions';
    }
    for (let i = 0; i < draft.children.length; i++) {
      const childError = validateQuestionDraft(draft.children[i], i + 1);
      if (childError) return childError;
    }
    return null;
  }
  if (draft.questionType === 'Underline') {
    if (!(draft.passageText || '').trim() || !(draft.correctAnswer || '').trim()) {
      return 'teacher.qbank.underlinePhrase';
    }
    return null;
  }
  if (isComplete(draft.questionType)) {
    if (!(draft.passageText || '').trim() || extractCompleteAnswers(draft.passageText).length === 0) {
      return 'teacher.qbank.completeBlanksRequired';
    }
    return null;
  }
  if (isTeacherGradedText(draft.questionType)) {
    return prompt ? null : 'teacher.qbank.required';
  }
  if (isMap(draft.questionType)) {
    if (!draft.promptImageMediaAssetId) return 'teacher.qbank.mapImageRequired';
    if (!draft.mapMarkers?.length) return 'teacher.qbank.mapMarkersRequired';
    if (draft.mapMarkers.some((marker) => !(marker.correctAnswer || '').trim())) {
      return 'teacher.qbank.mapAnswersRequired';
    }
    return null;
  }
  if (needsOptions(draft.questionType)) {
    const filled = filledOptions(draft.options);
    if (filled.length < 2) {
      return 'teacher.qbank.minOptions';
    }
    if (isOrder(draft.questionType)) {
      return null;
    }
    if (isMulti(draft.questionType)) {
      if (!draft.correctKeys.length) return 'teacher.qbank.selectMulti';
    } else if (!draft.correctAnswer) {
      return 'teacher.qbank.selectSingle';
    }
  }
  if (draft.questionType === 'TrueFalse' && !draft.correctAnswer) {
    return 'teacher.qbank.selectSingle';
  }
  return null;
}

export interface QuestionPayload {
  id?: string;
  prompt: string;
  questionType: string;
  passageText?: string;
  options?: string[];
  correctAnswer: string;
  correctOption: string;
  points: number;
  sortOrder: number;
  promptImageMediaAssetId?: string | null;
  mapMarkers?: { id: string; x: number; y: number; label: string; kind: string }[];
  children?: QuestionPayload[];
}

export function toQuestionPayload(draft: QuestionDraft, sortOrder: number): QuestionPayload {
  const type = draft.questionType;
  const filled = filledOptions(draft.options);
  let correct = draft.correctAnswer;
  if (isMulti(type)) correct = draft.correctKeys.join(',');
  if (isOrder(type)) correct = filled.map((option) => option.key).join(',');
  if (isMap(type)) correct = encodeMapAnswers(draft.mapMarkers || []);
  if (isComplete(type)) correct = encodeCompleteAnswers(extractCompleteAnswers(draft.passageText));
  if (isParagraph(type)) correct = '';
  return {
    id: draft.id || undefined,
    prompt: (draft.prompt || '').trim(),
    questionType: type,
    passageText: (draft.passageText || '').trim() || undefined,
    options: needsOptions(type) ? filled.map((option) => option.text) : undefined,
    correctAnswer: correct || '',
    correctOption: correct || '',
    points: draft.points > 0 ? draft.points : 1,
    sortOrder,
    promptImageMediaAssetId: draft.promptImageMediaAssetId || null,
    mapMarkers: isMap(type)
      ? (draft.mapMarkers || []).map((marker) => ({
          id: marker.id,
          x: marker.x,
          y: marker.y,
          label: marker.label,
          kind: marker.kind
        }))
      : undefined,
    children: isParagraph(type)
      ? draft.children.map((child, index) => toQuestionPayload(child, index + 1))
      : undefined
  };
}

export function draftFromAssignmentQuestion(question: AssignmentQuestion): QuestionDraft {
  return draftFromApi({
    id: question.id,
    prompt: question.prompt,
    questionType: question.questionType,
    passageText: question.passageText,
    options: question.options,
    optionA: question.optionA,
    optionB: question.optionB,
    optionC: question.optionC,
    correctAnswer: question.correctAnswer,
    points: question.points,
    promptImageMediaAssetId: question.promptImageMediaAssetId,
    promptImageUrl: question.promptImageUrl,
    mapMarkers: question.mapMarkers,
    children: question.children
  });
}

export function draftFromQuizQuestion(question: TeacherQuizQuestionDetail): QuestionDraft {
  return draftFromApi({
    id: question.id,
    prompt: question.prompt,
    questionType: question.questionType,
    passageText: question.passageText,
    options: question.options,
    correctAnswer: question.correctAnswer || question.correctOption,
    points: question.points,
    promptImageMediaAssetId: question.promptImageMediaAssetId,
    promptImageUrl: question.promptImageUrl,
    mapMarkers: question.mapMarkers,
    children: question.children
  });
}

function draftFromApi(question: {
  id?: string;
  prompt?: string | null;
  questionType?: string | null;
  passageText?: string | null;
  options?: ChoiceOption[] | null;
  optionA?: string | null;
  optionB?: string | null;
  optionC?: string | null;
  correctAnswer?: string | null;
  points?: number | null;
  promptImageMediaAssetId?: string | null;
  promptImageUrl?: string | null;
  mapMarkers?: { id: string; x: number; y: number; label: string; kind: string }[] | null;
  children?: AssignmentQuestion[] | TeacherQuizQuestionDetail[] | null;
}): QuestionDraft {
  const type = normalizeType(question.questionType);
  const optionTexts = question.options?.length
    ? question.options.map((option) => option.text)
    : [question.optionA, question.optionB, question.optionC].filter((text): text is string => !!text);
  const correct = question.correctAnswer || '';
  const mapAnswers = isMap(type) ? parseMapAnswers(correct) : {};
  const childSource = (question.children ?? []) as Array<AssignmentQuestion | TeacherQuizQuestionDetail>;
  return {
    id: question.id,
    prompt: question.prompt || '',
    questionType: type,
    passageText: question.passageText || '',
    options: optionTexts.length
      ? optionTexts.map((text) => ({ text }))
      : [{ text: '' }, { text: '' }],
    correctAnswer: isMulti(type) ? '' : correct,
    correctKeys: isMulti(type)
      ? correct
          .split(',')
          .map((key) => key.trim().toUpperCase())
          .filter(Boolean)
      : [],
    points: question.points && question.points > 0 ? question.points : 1,
    children: childSource.map((child) =>
      'correctOption' in child ? draftFromQuizQuestion(child) : draftFromAssignmentQuestion(child)
    ),
    promptImageMediaAssetId: question.promptImageMediaAssetId || null,
    promptImageUrl: question.promptImageUrl || null,
    mapMarkers: (question.mapMarkers || []).map((marker) => ({
      id: marker.id,
      x: marker.x,
      y: marker.y,
      label: marker.label || marker.id,
      kind: marker.kind === 'arrow' ? 'arrow' : 'number',
      correctAnswer:
        mapAnswers[marker.id] ||
        Object.entries(mapAnswers).find(([key]) => key.toLowerCase() === marker.id.toLowerCase())?.[1] ||
        ''
    }))
  };
}

export function draftFromGenerated(question: {
  prompt: string;
  questionType: string;
  options: string[];
  correctOption: string;
  correctAnswer: string;
}): QuestionDraft {
  const type =
    question.questionType === 'ShortAnswer'
      ? 'ShortAnswer'
      : question.questionType === 'MultipleChoice'
        ? 'SingleChoice'
        : normalizeType(question.questionType);
  const options = (question.options?.length ? question.options : ['', '']).map((text) => ({ text }));
  const correct = question.correctOption || question.correctAnswer || '';
  return {
    prompt: question.prompt,
    questionType: type,
    passageText: '',
    options,
    correctAnswer: type === 'MultiChoice' ? '' : correct,
    correctKeys:
      type === 'MultiChoice'
        ? correct
            .split(',')
            .map((key) => key.trim().toUpperCase())
            .filter(Boolean)
        : [],
    points: 1,
    children: [],
    mapMarkers: []
  };
}

export function plainPrompt(html: string): string {
  if (typeof document === 'undefined') return (html || '').replace(/<[^>]+>/g, ' ').trim();
  const el = document.createElement('div');
  el.innerHTML = html || '';
  return (el.textContent || '').trim();
}

export function flattenQuestions<T extends { id: string; questionType?: string; children?: T[] }>(questions: T[]): T[] {
  const list: T[] = [];
  for (const question of questions) {
    list.push(question);
    if (question.children?.length) {
      list.push(...flattenQuestions(question.children));
    }
  }
  return list;
}

export function answerableQuestions<T extends { id: string; questionType?: string; children?: T[] }>(questions: T[]): T[] {
  return flattenQuestions(questions).filter((question) => question.questionType !== 'Paragraph');
}
