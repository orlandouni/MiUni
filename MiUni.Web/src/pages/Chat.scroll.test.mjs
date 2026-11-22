import { test } from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { createRequire } from "node:module";
import { createElement, Fragment } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { createServer } from "vite";

// Use an existing browser installation without downloading one or calling the API.
// MIUNI_PLAYWRIGHT_MODULE: module name or absolute path to playwright/playwright-core.
// MIUNI_CHROMIUM_EXECUTABLE: optional path for an existing Chromium executable.
test("long assistant answers keep scrolling inside the conversation", {
  skip: !process.env.MIUNI_PLAYWRIGHT_MODULE && "Set MIUNI_PLAYWRIGHT_MODULE to run browser layout checks.",
}, async () => {
  const { chromium } = createRequire(import.meta.url)(process.env.MIUNI_PLAYWRIGHT_MODULE);
  const server = await createServer({ server: { middlewareMode: true, watch: null } });
  let browser;
  try {
    const { default: ChatAnswer } = await server.ssrLoadModule("/src/components/ChatAnswer.jsx");
    const messages = renderToStaticMarkup(createElement(Fragment, null,
      ...Array.from({ length: 40 }, (_, index) => createElement("div", {
        key: index, className: "chat-message asistente",
      }, createElement("div", { className: "chat-bubble" }, createElement(ChatAnswer, {
        messageId: `answer-${index}`, timestamp: "12:00",
        content: `## Answer ${index}\n\n${"A long answer with enough detail to require scrolling. ".repeat(45)}`,
      }))))));
    const styles = (await Promise.all([
      "../styles/design-system.css", "../styles/base.css", "../styles/chat.css", "./Chat.css",
    ].map(path => readFile(new URL(path, import.meta.url), "utf8"))))
      .map(css => css.replace(/@import[^;]+;/g, "")).join("\n");
    browser = await chromium.launch({ headless: true,
      ...(process.env.MIUNI_CHROMIUM_EXECUTABLE && { executablePath: process.env.MIUNI_CHROMIUM_EXECUTABLE }),
    });
    for (const viewport of [{ width: 1280, height: 720 }, { width: 390, height: 844 }]) {
      const page = await browser.newPage({ viewport, reducedMotion: "reduce" });
      try {
        // Keep the production scroll hierarchy and render the real answer component,
        // including its otherwise invisible live status region.
        await page.setContent(`<style>${styles}</style><div id="root"><div class="route-frame">
          <main class="chat-page"><section class="chat-main">
            <header class="chat-topbar">Assistant</header><div class="chat-content">
              <section class="chat-intro compacta"><h1>Conversation</h1></section>
              <div class="chat-conversation"><div class="chat-messages" tabindex="0">${messages}</div></div>
              <form class="chat-composer"><textarea aria-label="Message"></textarea><button>Send</button></form>
            </div></section></main></div></div>`);
        const panel = page.locator(".chat-messages");
        await panel.evaluate(element => { element.scrollTop = element.scrollHeight; });
        await panel.hover();
        await page.mouse.wheel(0, 100000);
        // Sample after the wheel event has been processed and composited.
        await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const layout = await page.evaluate(() => {
          const messages = document.querySelector(".chat-messages");
          const header = document.querySelector(".chat-topbar").getBoundingClientRect();
          const composer = document.querySelector(".chat-composer").getBoundingClientRect();
          return { windowY: scrollY, top: document.querySelector(".chat-page").getBoundingClientRect().top,
            innerScrollable: messages.scrollHeight > messages.clientHeight,
            bottomReached: messages.scrollTop + messages.clientHeight >= messages.scrollHeight - 1,
            headerVisible: header.top >= 0 && header.bottom <= innerHeight,
            composerVisible: composer.top >= 0 && composer.bottom <= innerHeight,
          };
        });
        assert.deepEqual(layout, { windowY: 0, top: 0, innerScrollable: true,
          bottomReached: true, headerVisible: true, composerVisible: true }, JSON.stringify(viewport));
        // The history must remain readable in the opposite direction as well.
        await panel.evaluate(element => { element.scrollTop = 0; });
        assert.equal(await panel.evaluate(element => element.scrollTop), 0);
      } finally { await page.close(); }
    }
  } finally {
    await browser?.close();
    await server.close();
  }
});
