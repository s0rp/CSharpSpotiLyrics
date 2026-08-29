import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'CSharpSpotiLyrics',
  tagline: 'Lightweight Spotify Lyrics & Canvas API Client for C#',
  favicon: 'img/favicon.ico',

  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },
  headTags: [
    {
      tagName: 'link',
      attributes: {
        rel: 'alternate',
        type: 'text/markdown',
        title: 'LLM-friendly version',
        href: '/llms.txt',
      },
    },
  ],
  url: 'https://cssldocs.sxrp.me',
  baseUrl: '/',

  // GitHub pages deployment config.
  organizationName: 's0rp',
  projectName: 'CSharpSpotiLyrics',

  onBrokenLinks: 'warn', // Set to warn for smoother initial builds

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs', // Points to the /docs folder in the repository root
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/s0rp/CSharpSpotiLyrics/tree/main/',
        },
        blog: false, // Disabled the blog to keep the focus purely on C# Library Docs
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  plugins: [
    [
      'docusaurus-plugin-llms',
      {
        generateLLMsTxt: true,       // Automatically generates /llms.txt
        generateLLMsFullTxt: true,   // Automatically generates /llms-full.txt
        docsDir: '../docs',          // Scanning target folder
        title: 'CSharpSpotiLyrics',
        description: 'Lightweight Spotify Lyrics & Canvas API Client for C# without Playwright',
      },
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'CSharpSpotiLyrics',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/s0rp/CSharpSpotiLyrics',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'API Reference',
              to: '/docs/API',
            },
            {
              label: 'Exceptions',
              to: '/docs/EXCEPTIONS',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/s0rp/CSharpSpotiLyrics',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} s0rp. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp'], // Added C# syntax highlighting support
    },
  } satisfies Preset.ThemeConfig,
};

export default config;