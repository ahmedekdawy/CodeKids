/** Builds a wa.me share URL with the student assessment link, title, grade, and course. */
export function assessmentWhatsAppShareUrl(options: {
  kindLabel: string;
  name: string;
  gradeLabel?: string | null;
  courseLabel?: string | null;
  studentPath: string;
  gradeCaption: string;
  courseCaption: string;
}): string {
  const lines = [`${options.kindLabel}: ${options.name}`];
  const grade = options.gradeLabel?.trim();
  const course = options.courseLabel?.trim();
  if (grade) {
    lines.push(`${options.gradeCaption} ${grade}`);
  }
  if (course) {
    lines.push(`${options.courseCaption} ${course}`);
  }
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  lines.push('', `${origin}${options.studentPath}`);
  return `https://wa.me/?text=${encodeURIComponent(lines.join('\n'))}`;
}
