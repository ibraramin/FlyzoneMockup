// Landing hero map. Leaflet 1.9.4 is served locally from wwwroot/lib so the
// page stays inside the CSP's script-src 'self' and style-src 'self' rules.
// Tiles come from the keyless OpenStreetMap standard endpoint, with the
// required attribution; no API key or tile-provider watermark is involved.
document.addEventListener("DOMContentLoaded", function () {
    var container = document.getElementById("map-container");
    if (!container || typeof L === "undefined") {
        return;
    }

    var map = L.map("map-container", {
        zoomControl: false,
        scrollWheelZoom: false
    }).setView([24.5, 54], 4.25);

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 18,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    var locations = [
        { name: "Dubai",        coords: [25.2048, 55.2708], tone: "dot-red" },
        { name: "Ajman",        coords: [25.4052, 55.4481], tone: "dot-green", labelClass: "label-above" },
        { name: "Saudi Arabia", coords: [23.8859, 45.0792], tone: "dot-red" },
        { name: "Qatar",        coords: [25.2854, 51.5310], tone: "dot-red" },
        { name: "India",        coords: [20.5937, 78.9629], tone: "dot-red" },
        { name: "Bangladesh",   coords: [23.8103, 90.4125], tone: "dot-red" }
    ];

    locations.forEach(function (loc) {
        var icon = document.createElement("div");
        icon.className = "custom-leaflet-icon";

        var dot = document.createElement("span");
        dot.className = "map-dot " + loc.tone;

        var label = document.createElement("span");
        label.className = "map-label" + (loc.labelClass ? " " + loc.labelClass : "");
        label.textContent = loc.name;

        icon.appendChild(dot);
        icon.appendChild(label);

        L.marker(loc.coords, {
            icon: L.divIcon({
                html: icon.outerHTML,
                className: "custom-leaflet-icon",
                iconSize: [14, 14],
                iconAnchor: [7, 7]
            })
        }).addTo(map);
    });
});
