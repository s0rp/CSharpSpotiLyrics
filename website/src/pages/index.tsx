import {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import Head from '@docusaurus/Head'; 

import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/API">
            Get Started 🚀
          </Link>
        </div>
      </div>
    </header>
  );
}

interface FeatureItem {
  title: string;
  description: ReactNode;
}

const FeatureList: FeatureItem[] = [
  {
    title: 'Zero Headless Browsers 🌐',
    description: (
      <>
        No Playwright or Selenium required. Built with lightweight HTTP clients 
        and optimized regex parsing to run seamlessly on any platform.
      </>
    ),
  },
  {
    title: 'High Performance & Stream-Based ⚡',
    description: (
      <>
        Features stream-based JSON parsing and custom task throttling using SemaphoreSlim 
        to prevent CPU/RAM spikes and ensure thread-safe operations.
      </>
    ),
  },
  {
    title: 'Rich Spotify APIs 🎶',
    description: (
      <>
        Retrieve line-synced lyrics, raw Canvas MP4 loop animations, verified artist details, 
        and player states out of the box.
      </>
    ),
  },
];

function Feature({title, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center padding-horiz--md margin-top--lg">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title} - Spotify Lyrics & Canvas API Client`}
      description="Lightweight, dependency-free Spotify Lyrics and Canvas fetcher for C# and .NET Standard without Playwright">
      
      <Head>
        <meta name="keywords" content="ai-friendly, claudecode, llms-txt, lyrics, lyrics-api, lyrics-fetcher, lyrics-finder, lyrics-generator, lyrics-scraping, lyrics-search, spotify, spotify-lyrics, spotify-lyrics-fetcher, spotifyapi, spotifylyrics" />
        <meta property="og:title" content="CSharpSpotiLyrics - Synced Spotify Lyrics & Canvas API for .NET" />
        <meta property="og:description" content="A highly optimized, dependency-free C# and .NET Standard library to fetch synced Spotify lyrics and Canvas MP4 loops without Playwright or headless browsers." />
        <meta property="og:type" content="website" />
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="robots" content="index, follow" />
      </Head>

      <HomepageHeader />
      <main>
        <section className="margin-vertical--xl">
          <div className="container">
            <div className="row">
              {FeatureList.map((props, idx) => (
                <Feature key={idx} {...props} />
              ))}
            </div>
          </div>
        </section>
      </main>
    </Layout>
  );
}