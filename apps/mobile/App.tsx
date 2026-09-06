import {
  Montserrat_400Regular,
  Montserrat_500Medium,
  Montserrat_600SemiBold,
  Montserrat_700Bold,
  Montserrat_800ExtraBold,
  useFonts,
} from '@expo-google-fonts/montserrat';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import React, { useCallback } from 'react';
import { View } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthProvider } from './src/context/AuthContext';
import { I18nProvider } from './src/i18n';
import { RootNavigator } from './src/navigation/RootNavigator';
import { colors } from './src/theme';

// L'écran de lancement natif reste affiché jusqu'à ce que l'application
// ait vraiment quelque chose à montrer. Sans cet appel, le système le
// retire dès que la vue racine existe — c'est-à-dire avant que Montserrat
// ne soit chargée : on voit alors le premier écran en police système,
// puis un saut quand la vraie police arrive.
SplashScreen.preventAutoHideAsync().catch(() => {
  // Déjà masqué, ou appelé deux fois en rechargement à chaud. Sans
  // conséquence : le pire cas est un écran de lancement plus court.
});

// Le fondu adoucit le passage du lancement à l'application. Court, parce
// qu'une animation d'ouverture qu'on remarque est une animation trop
// longue.
SplashScreen.setOptions({ duration: 250, fade: true });

export default function App() {
  // Montserrat est la police de la charte. Elle est embarquée dans le
  // binaire, pas téléchargée : sur un réseau EDGE, une police distante
  // arrive après le premier écran, ou jamais.
  const [fontsLoaded, fontError] = useFonts({
    Montserrat_400Regular,
    Montserrat_500Medium,
    Montserrat_600SemiBold,
    Montserrat_700Bold,
    Montserrat_800ExtraBold,
  });

  // On attend le chargement, mais jamais une erreur de police : si un
  // fichier manque, l'application s'affiche avec la police système plutôt
  // que de rester bloquée derrière l'écran de lancement.
  const ready = fontsLoaded || fontError !== null;

  // Le masquage est déclenché à la POSE de la vue racine, pas dans un
  // effet : un effet s'exécute avant que la première image ne soit
  // dessinée, et l'écran de lancement disparaîtrait sur une frame vide.
  const onLayout = useCallback(() => {
    if (ready) {
      SplashScreen.hideAsync().catch(() => {
        // Idem : rien à rattraper si le système l'a déjà retiré.
      });
    }
  }, [ready]);

  if (!ready) return null;

  return (
    <SafeAreaProvider>
      <View style={{ flex: 1, backgroundColor: colors.ground }} onLayout={onLayout}>
        <I18nProvider>
          <AuthProvider>
            <RootNavigator />
            {/* Fond clair : la barre d'état doit être en contenu sombre. */}
            <StatusBar style="dark" />
          </AuthProvider>
        </I18nProvider>
      </View>
    </SafeAreaProvider>
  );
}
