import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from '../i18n';
import { DevicesScreen } from '../screens/DevicesScreen';
import { HomeScreen } from '../screens/HomeScreen';
import { KycCaptureScreen } from '../screens/KycCaptureScreen';
import { LoginScreen } from '../screens/LoginScreen';
import { ProfileScreen } from '../screens/ProfileScreen';
import { RegisterScreen } from '../screens/RegisterScreen';
import { TopupScreen } from '../screens/TopupScreen';
import { colors } from '../theme';

const Stack = createNativeStackNavigator();

function AuthStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Login" component={LoginScreen} />
      <Stack.Screen name="Register" component={RegisterScreen} />
    </Stack.Navigator>
  );
}

function AppStack() {
  const { t } = useTranslation();
  return (
    <Stack.Navigator>
      <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Lajan’m' }} />
      <Stack.Screen name="Topup" component={TopupScreen} options={{ title: t('topup.title') }} />
      <Stack.Screen name="Kyc" component={KycCaptureScreen} options={{ title: t('kyc.title') }} />
      <Stack.Screen name="Profile" component={ProfileScreen} options={{ title: t('profile.title') }} />
      <Stack.Screen
        name="Devices"
        component={DevicesScreen}
        options={{ title: t('profile.devices_title') }}
      />
    </Stack.Navigator>
  );
}

export function RootNavigator() {
  const { user, isReady } = useAuth();

  if (!isReady) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.background }}>
        <ActivityIndicator color={colors.primary} />
      </View>
    );
  }

  return <NavigationContainer>{user ? <AppStack /> : <AuthStack />}</NavigationContainer>;
}
