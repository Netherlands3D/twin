# Protofunk optimized preview deployment

The branch `feature/REALLOCATE-pre-oplevering` produces two independent WebGL builds:

- `viewer-development`: the existing Netherlands3D development build with Unity's
  `-Development -AllowDebugging` flags.
- `viewer-reallocate-preview`: an additional optimized build without those flags.

The optimized build can be deployed to the external repository
`Protofunk/twin-reallocate`. The Netherlands3D production build, Docker image, and
GitHub Pages deployment remain restricted to `main` and are not changed by this
preview deployment.

## External repository setup

1. Create the public repository `Protofunk/twin-reallocate`.
2. Add a repository-specific SSH deploy key to that repository with write access.
3. Store the matching private key in the Netherlands3D `twin` repository as the
   Actions secret `PROTOFUNK_PAGES_DEPLOY_KEY`.
4. In the Netherlands3D `twin` repository, add the Actions variable
   `PROTOFUNK_PAGES_ENABLED` with the value `true`.
5. Push `feature/REALLOCATE-pre-oplevering`. The workflow creates or updates the
   `gh-pages` branch in `Protofunk/twin-reallocate`.
6. In `Protofunk/twin-reallocate`, configure GitHub Pages to deploy from the
   `gh-pages` branch and the repository root.

The resulting viewer URL is:

`https://protofunk.github.io/twin-reallocate/`

The deploy key should only be authorized for `Protofunk/twin-reallocate`. Do not
reuse a personal access token or a key that can write to other repositories.

## Disabling future writes

For a reversible pause, change `PROTOFUNK_PAGES_ENABLED` to `false` or remove the
variable from the Netherlands3D repository. The build artifacts will still be
created, but the external deployment job will be skipped.

When the project is complete, fully revoke write access by deleting the
`NL3D optimized preview deployment` deploy key from `Protofunk/twin-reallocate`
and deleting the `PROTOFUNK_PAGES_DEPLOY_KEY` Actions secret from
`Netherlands3D/twin`.

## Source backup

The Pages workflow publishes compiled WebGL output to `gh-pages`; it does not copy
the Unity source repository. To retain a source backup as well, push the
`feature/REALLOCATE-pre-oplevering` branch to the external repository's `main` or
`source` branch. The deployment workflow never writes to that source branch.
