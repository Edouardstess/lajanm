import {
  Montserrat_400Regular,
  Montserrat_500Medium,
  Montserrat_600SemiBold,
  Montserrat_700Bold,
  Montserrat_800ExtraBold,
  useFonts,
} from '@expo-google-fonts/montserrat';
import { StatusBar } from 'expo-status-bar';
import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthProvider } from './src/context/AuthContext';
import { I18nProvider } from './src/i18n';
import { RootNavigator } from './src/navigation/RootNavigator';
import { colors } from './src/theme';

export default function App() {
  // Montserrat est la police de la charte de marque. Elle est embarquée
  // dans le binaire, pas téléchargée : sur un réseau EDGE, une police
  // distante arrive après le premier écran, ou jamais.
  const [fontsLoaded, fontError] = useFonts({
    Montserrat_400Regular,
    Montserrat_500Medium,
    Montserrat_600SemiBold,
    Montserrat_700Bold,
    Montserrat_800ExtraBold,
  });

  // On attend le chargement, mais jamais une erreur de police : si un
  // fichier manque, l'application s'affiche avec la police système plutôt
  // que de rester bloquée sur un écran vide.
  if (!fontsLoaded && !fontError) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.ground }}>
        <ActivityIndicator color={colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaProvider>
      <I18nProvider>
        <AuthProvider>
          <RootNavigator />
          {/* Fond clair : la barre d'état doit être en contenu sombre. */}
          <StatusBar style="dark" />
        </AuthProvider>
      </I18nProvider>
    </SafeAreaProvider>
  );
}
