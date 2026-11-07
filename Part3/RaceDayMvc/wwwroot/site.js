document.querySelectorAll("form[data-confirm]").forEach(form => form.addEventListener("submit", e => {
    if (!confirm(form.dataset.confirm)) e.preventDefault();
}));
const role = document.getElementById("registration-role");
const details = document.getElementById("participant-fields");
if (role && details) {
    const update = () => {
        details.hidden = role.value !== "Participant";
        details.querySelectorAll("input,select").forEach(input => { input.disabled = details.hidden; input.required = !details.hidden; });
    };
    role.addEventListener("change", update); update();
}
