// Self-contained service-region graphic for the landing hero.
// No tile provider, API key or third-party request: the marker positions are
// fixed and the backdrop is drawn in CSS, so the hero renders identically
// offline, in CI screenshots and behind a locked-down CSP.
document.addEventListener("DOMContentLoaded", function () {
    var container = document.getElementById("map-container");
    if (!container) {
        return;
    }

    var backdrop = document.createElement("div");
    backdrop.className = "fz-map-backdrop";
    container.appendChild(backdrop);

    var locations = [
        { name: "Saudi Arabia", left: "27%", top: "52%" },
        { name: "Qatar", left: "50%", top: "55%" },
        { name: "Dubai", left: "58%", top: "48%" },
        { name: "Ajman", left: "60%", top: "44%" },
        { name: "India", left: "73%", top: "62%" },
        { name: "Bangladesh", left: "84%", top: "55%" }
    ];

    locations.forEach(function (loc) {
        var marker = document.createElement("div");
        marker.className = "fz-map-marker";
        marker.style.left = loc.left;
        marker.style.top = loc.top;

        var dot = document.createElement("span");
        dot.className = "map-dot dot-red";

        var label = document.createElement("span");
        label.className = "map-label";
        label.textContent = loc.name;

        marker.appendChild(dot);
        marker.appendChild(label);
        backdrop.appendChild(marker);
    });
});
