const courseGrid = document.querySelector("#courseGrid");
const sessionList = document.querySelector("#sessionList");
const liveSessions = document.querySelector("#liveSessions");
const loadSessions = document.querySelector("#loadSessions");
const partnerForm = document.querySelector("#partnerForm");
const formMessage = document.querySelector("#formMessage");

/**
 * Safely set text content on an element.
 * Never uses innerHTML with external data to prevent XSS.
 */
function setText(el, value) {
  el.textContent = value;
}

function createPill(text) {
  const span = document.createElement("span");
  span.className = "pill";
  setText(span, text);
  return span;
}

function createCourseCard(course) {
  const article = document.createElement("article");
  article.className = "course-card";

  // Meta pills
  const meta = document.createElement("div");
  meta.className = "meta";
  meta.appendChild(createPill(`Age ${Number(course.minimumAge)}+`));
  meta.appendChild(createPill(`${Number(course.priceEgp)} EGP`));
  meta.appendChild(createPill(`${Number(course.coreSessions)}+${Number(course.supportSessions)}`));
  article.appendChild(meta);

  // Title
  const h3 = document.createElement("h3");
  setText(h3, course.title);
  article.appendChild(h3);

  // Short description
  const desc = document.createElement("p");
  setText(desc, course.shortDescription);
  article.appendChild(desc);

  // Outcome
  const outcome = document.createElement("p");
  const strong = document.createElement("strong");
  setText(strong, "Build:");
  outcome.appendChild(strong);
  outcome.append(` ${course.outcome}`);
  article.appendChild(outcome);

  // Enroll button
  const btn = document.createElement("button");
  btn.className = "button secondary";
  btn.dataset.courseId = String(course.id);
  btn.dataset.courseTitle = String(course.title);
  setText(btn, "Start enrollment");
  btn.addEventListener("click", () => enroll(course.id, course.title));
  article.appendChild(btn);

  return article;
}

async function loadCourses() {
  if (!courseGrid) return;
  const response = await fetch("/api/courses");
  const courses = await response.json();

  courseGrid.innerHTML = "";
  courses.forEach(course => courseGrid.appendChild(createCourseCard(course)));
}

async function enroll(courseId, title) {
  const studentName = prompt(`Student name for ${title}`);
  const studentEmail = prompt("Student email");
  if (!studentName || !studentEmail) return;

  const response = await fetch("/api/enrollments", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ courseId, studentName, studentEmail, promoCode: "PARTNER15" })
  });
  const result = await response.json();
  alert(`Enrollment lead created: ${result.courseTitle}, ${result.finalPriceEgp} EGP`);
}

function createSessionCard(session) {
  const article = document.createElement("article");
  article.className = "session";

  const strong = document.createElement("strong");
  setText(strong, session.title);
  article.appendChild(strong);

  const courseTitle = document.createElement("span");
  setText(courseTitle, session.courseTitle);
  article.appendChild(courseTitle);

  const meta = document.createElement("p");
  setText(meta, `${new Date(session.startsAt).toLocaleString()} · ${session.hostName}`);
  article.appendChild(meta);

  const link = document.createElement("a");
  link.className = "button secondary";
  // session.id is set via dataset to avoid injecting raw values into href strings
  const safeId = encodeURIComponent(String(session.id));
  link.href = `/live.html?session=${safeId}`;
  setText(link, "Join embedded classroom");
  article.appendChild(link);

  return article;
}

async function renderSessions(target) {
  if (!target) return;
  const response = await fetch("/api/live-sessions/upcoming");
  const sessions = await response.json();

  target.innerHTML = "";
  sessions.forEach(session => target.appendChild(createSessionCard(session)));
}

partnerForm?.addEventListener("submit", async event => {
  event.preventDefault();
  const payload = Object.fromEntries(new FormData(partnerForm).entries());
  const response = await fetch("/api/partnerships/leads", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  formMessage.textContent = response.ok
    ? "Partnership request received. The academy team can follow up from SQL Server."
    : "Please check the form details and try again.";
  if (response.ok) partnerForm.reset();
});

loadSessions?.addEventListener("click", () => renderSessions(liveSessions));

loadCourses();
renderSessions(sessionList);
renderSessions(liveSessions);
