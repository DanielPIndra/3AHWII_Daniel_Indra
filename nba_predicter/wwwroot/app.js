const matchupsEl = document.querySelector("#matchups");
const gamesEl = document.querySelector("#games");
const standingsEl = document.querySelector("#standings");
const conferenceButtons = document.querySelectorAll(".conference-button");
const matchupTemplate = document.querySelector("#matchup-template");
const gameTemplate = document.querySelector("#game-template");
let currentData = null;
let selectedConference = "Eastern Conference";

function pct(value) {
  return `${Number(value).toFixed(1)}%`;
}

function metric(label, value, suffix = "") {
  return `<div class="metric"><span>${label}</span><strong>${value}${suffix}</strong></div>`;
}

function logoUrl(teamId) {
  return `https://cdn.nba.com/logos/nba/${teamId}/global/L/logo.svg`;
}

function teamLogo(team, className = "") {
  const fallback = team.abbreviation || "NBA";
  return `
    <span class="logo-badge ${className}">
      <img src="${team.logoUrl || logoUrl(team.teamId)}" alt="${team.name} Logo" loading="lazy" onerror="this.style.display='none'; this.nextElementSibling.hidden=false;">
      <strong hidden>${fallback}</strong>
    </span>
  `;
}

function renderTeam(team, index) {
  const metrics = team.metrics;
  return `
    <section class="team-panel ${index === 0 ? "home" : "away"}">
      <div class="team-title">
        ${teamLogo(team, "team-logo")}
        <div>
          <h3>${team.name}</h3>
          <p>Seed ${team.seed || "-"} - ${team.wins}-${team.losses}</p>
        </div>
      </div>
      <div class="score-row">
        <div><span>Gesamt</span><strong>${team.totalScore}</strong></div>
        <div><span>Offense</span><strong>${team.offenseScore}</strong></div>
        <div><span>Defense</span><strong>${team.defenseScore}</strong></div>
      </div>
      <div class="metric-grid">
        ${metric("OffRtg", metrics.offRating)}
        ${metric("DefRtg", metrics.defRating)}
        ${metric("NetRtg", metrics.netRating)}
        ${metric("eFG", metrics.efgPct, "%")}
        ${metric("TS", metrics.tsPct, "%")}
        ${metric("AST/TO", metrics.astTo)}
        ${metric("DREB", metrics.drebPct, "%")}
      </div>
    </section>
  `;
}

function formatGameTime(value) {
  if (!value) return "Zeit offen";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString("de-AT", {
    weekday: "short",
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function renderGames(games) {
  gamesEl.innerHTML = "";
  if (!games || games.length === 0) {
    gamesEl.innerHTML = '<div class="loading">Es wurden keine kommenden Playoff-Spiele geladen.</div>';
    return;
  }

  games.forEach((game) => {
    const card = gameTemplate.content.cloneNode(true);
    card.querySelector(".conference").textContent = `${formatGameTime(game.gameTimeUtc)} - ${game.status}`;
    card.querySelector("h3").innerHTML = `
      <span class="inline-team">
        ${teamLogo(game.awayTeam, "mini-logo")}
        ${game.awayTeam.name}
      </span>
      <span class="at">@</span>
      <span class="inline-team">
        ${teamLogo(game.homeTeam, "mini-logo")}
        ${game.homeTeam.name}
      </span>
    `;
    card.querySelector(".favorite strong").textContent = game.favorite;
    card.querySelector(".home-ratio span").textContent = `${game.homeTeam.abbreviation} Win-Ratio`;
    card.querySelector(".home-ratio strong").textContent = pct(game.homeWinRatio);
    card.querySelector(".away-ratio span").textContent = `${game.awayTeam.abbreviation} Win-Ratio`;
    card.querySelector(".away-ratio strong").textContent = pct(game.awayWinRatio);
    card.querySelector(".home-fill").style.width = `${game.homeWinRatio}%`;
    card.querySelector(".away-fill").style.width = `${game.awayWinRatio}%`;
    gamesEl.appendChild(card);
  });
}

function renderStandings(data) {
  const table = data?.standings?.find((item) => item.name === selectedConference);
  if (!table || !table.rows || table.rows.length === 0) {
    standingsEl.innerHTML = '<div class="loading">Keine Conference-Tabelle verfügbar.</div>';
    return;
  }

  standingsEl.innerHTML = `
    <div class="standings-head">
      <span>#</span>
      <span>Team</span>
      <span>W-L</span>
      <span>Rating</span>
      <span>Off</span>
      <span>Def</span>
    </div>
    ${table.rows.map((row) => `
      <div class="standings-row">
        <span class="seed">${row.seed}</span>
        <span class="standings-team">
          ${teamLogo(row.team, "mini-logo")}
          <strong>${row.team.name}</strong>
        </span>
        <span>${row.wins}-${row.losses}</span>
        <span>${row.totalScore}</span>
        <span>${row.offenseScore}</span>
        <span>${row.defenseScore}</span>
      </div>
    `).join("")}
  `;
}

function renderMatchups(data) {
  currentData = data;
  document.querySelector("#season").textContent = data.season;
  document.querySelector("#source").textContent = data.source;
  document.querySelector("#offense-weight").textContent = Math.round(data.weights.offense * 100);
  document.querySelector("#defense-weight").textContent = Math.round(data.weights.defense * 100);

  renderStandings(data);
  renderGames(data.games);
  matchupsEl.innerHTML = "";
  data.matchups.forEach((matchup) => {
    const card = matchupTemplate.content.cloneNode(true);
    card.querySelector(".conference").textContent = `${matchup.conference || "NBA"} - Serie ${matchup.series}`;
    card.querySelector("h2").textContent = `${matchup.teams[0].name} vs ${matchup.teams[1].name}`;
    card.querySelector(".favorite strong").textContent = `${matchup.favorite} - ${pct(matchup.favoriteProbability)}`;
    card.querySelector(".team-grid").innerHTML = matchup.teams.map(renderTeam).join("");
    const meter = card.querySelector(".meter span");
    meter.style.width = `${matchup.favoriteProbability}%`;
    meter.textContent = `Edge ${matchup.edge}`;
    matchupsEl.appendChild(card);
  });
}

conferenceButtons.forEach((button) => {
  button.addEventListener("click", () => {
    selectedConference = button.dataset.conference;
    conferenceButtons.forEach((item) => item.classList.toggle("active", item === button));
    renderStandings(currentData);
  });
});

async function load() {
  gamesEl.innerHTML = '<div class="loading">Kommende Playoff-Spiele werden geladen...</div>';
  matchupsEl.innerHTML = '<div class="loading">Playoff-Daten werden geladen...</div>';
  try {
    const response = await fetch("/api/playoffs");
    if (!response.ok) {
      throw new Error("API konnte nicht geladen werden");
    }
    renderMatchups(await response.json());
  } catch (error) {
    gamesEl.innerHTML = `<div class="loading error">${error.message}</div>`;
    matchupsEl.innerHTML = `<div class="loading error">${error.message}</div>`;
  }
}

load();
