const courseGrid = document.querySelector("#courseGrid");
const sessionList = document.querySelector("#sessionList");
const liveSessions = document.querySelector("#liveSessions");
const loadSessions = document.querySelector("#loadSessions");
const partnerForm = document.querySelector("#partnerForm");
const formMessage = document.querySelector("#formMessage");

async function loadCourses() {
  if (!courseGrid) return;
  const response = await fetch("/api/courses");
  const courses = await response.json();
  courseGrid.innerHTML = courses.map(course => `
    <article class="course-card">
      <div class="meta">
        <span class="pill">Age ${course.minimumAge}+</span>
        <span class="pill">${course.priceEgp} EGP</span>
        <span class="pill">${course.coreSessions}+${course.supportSessions}</span>
      </div>
      <h3>${course.title}</h3>
      <p>${course.shortDescription}</p>
      <p><strong>Build:</strong> ${course.outcome}</p>
      <button class="button secondary" data-course-id="${course.id}" data-course-title="${course.title}">Start enrollment</button>
    </article>
  `).join("");

  courseGrid.querySelectorAll("button[data-course-id]").forEach(button => {
    button.addEventListener("click", () => enroll(button.dataset.courseId, button.dataset.courseTitle));
  });
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

async function renderSessions(target) {
  if (!target) return;
  const response = await fetch("/api/live-sessions/upcoming");
  const sessions = await response.json();
  target.innerHTML = sessions.map(session => `
    <article class="session">
      <strong>${session.title}</strong>
      <span>${session.courseTitle}</span>
      <p>${new Date(session.startsAt).toLocaleString()} · ${session.hostName}</p>
      <a class="button secondary" href="/live.html?session=${session.id}">Join embedded classroom</a>
    </article>
  `).join("");
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
