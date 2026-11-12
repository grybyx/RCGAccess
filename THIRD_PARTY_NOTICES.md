# Third-party components

The release ZIP of RCGAccess bundles the following components unchanged. They are not part of this repository's source code.

| Component | Version | License | Source |
| --- | --- | --- | --- |
| BepInEx (`BepInEx/core`) | 5.4.23.2 | LGPL-2.1 | https://github.com/BepInEx/BepInEx |
| UnityDoorstop (`winhttp.dll`, `doorstop_config.ini`) | 4.3.0 | LGPL-2.1 | https://github.com/NeighTools/UnityDoorstop |
| HarmonyX (`0Harmony.dll`) | shipped with BepInEx 5.4.23.2 | MIT | https://github.com/BepInEx/HarmonyX |
| MonoMod, Mono.Cecil | shipped with BepInEx 5.4.23.2 | MIT | https://github.com/MonoMod/MonoMod, https://github.com/jbevain/cecil |
| Prism by Ethin Probst (`RCGAccess/prism.dll`, Windows x64) | 0.18.3 | MPL-2.0 | https://github.com/ethindp/prism |

The source code of these components is available at the links above. You can replace them with your own builds of the same versions.

Prism (prism.dll) is unmodified and licensed under the Mozilla Public License 2.0. Its source code is available at https://github.com/ethindp/prism (release v0.18.3). The release ZIP contains its license text and NOTICE, with the licenses of the libraries it contains, in `RCGAccess/licenses/prism/`. RCGAccess calls it from `speech/PrismOut.cs`.

River City Girls is © WayForward / Arc System Works. RCGAccess is an unofficial fan mod and contains no game files.
