import fs from 'fs';
const p = 'src/app/pages/admin/smart-assistant-admin.component.html';
let d = fs.readFileSync(p, 'utf8');

const startTag = '<select id="user-role" class="form-select" [(ngModel)]="userFormRole">';
const start = d.indexOf(startTag);
if (start < 0) {
  console.log('start tag not found');
  process.exit(1);
}

const endTag = '</select>';
const end = d.indexOf(endTag, start);
if (end < 0) {
  console.log('end tag not found');
  process.exit(1);
}

const replacement = `<select id="user-role" class="form-select" [(ngModel)]="userFormRole" (change)="setUsersRoleString($any($event.target).value)">
          <option value="teacher">{{ 'role.teacher' | t }}</option>
          <option value="parent">{{ 'role.parent' | t }}</option>
          <option value="student">{{ 'role.student' | t }}</option>
        </select>`;

d = d.slice(0, start) + replacement + d.slice(end + endTag.length);
fs.writeFileSync(p, d);

const chk = fs.readFileSync(p, 'utf8');
const i = chk.indexOf('<select id="user-role"');
const blockEnd = chk.indexOf('</select>', i);
console.log('bytes', d.length);
console.log('block now', JSON.stringify(chk.slice(i, blockEnd + '</select>'.length)));
