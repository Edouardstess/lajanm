import React from 'react';
import { Text, View, ViewStyle } from 'react-native';
import { colors } from '../theme';

export type IconName =
  | 'arrow-up'
  | 'arrow-down'
  | 'arrow-right'
  | 'send'
  | 'check'
  | 'clock'
  | 'lock'
  | 'person'
  | 'home'
  | 'help'
  | 'card'
  | 'alert'
  | 'plus';

interface Props {
  name: IconName;
  size?: number;
  color?: string;
  style?: ViewStyle;
}

/**
 * Jeu d'icônes dessiné avec les primitives de React Native.
 *
 * Pourquoi pas une bibliothèque : l'app n'embarque pas `react-native-svg`
 * ni de police d'icônes, et en ajouter une oblige à reconstruire le
 * binaire natif — pour un gain purement décoratif, chaque libellé étant
 * déjà écrit en toutes lettres à côté de son icône (public à faible
 * littératie numérique : l'icône appuie le mot, elle ne le remplace
 * jamais). Ces formes sont donc composées de bordures et de rotations.
 *
 * Elles sont décoratives au sens de l'accessibilité : `importantFor-
 * Accessibility="no"` évite que le lecteur d'écran annonce une vue vide
 * avant le libellé qui, lui, porte le sens.
 */
export function Icon({ name, size = 20, color = colors.text, style }: Props) {
  // À 13 px, un trait de 2 px remplit la forme et le glyphe devient une
  // tache. Le trait suit donc la taille, avec un plancher plus bas.
  const stroke = Math.max(1.4, size * 0.095);
  const box: ViewStyle = { width: size, height: size, alignItems: 'center', justifyContent: 'center' };

  return (
    <View style={[box, style]} importantForAccessibility="no" accessibilityElementsHidden>
      {renderGlyph(name, size, color, stroke)}
    </View>
  );
}

function renderGlyph(name: IconName, size: number, color: string, stroke: number) {
  switch (name) {
    // Une hampe verticale et un chevron : la direction porte le sens
    // (entrant / sortant) dans l'historique comme sur l'accueil.
    case 'arrow-down':
    case 'arrow-up': {
      const up = name === 'arrow-up';
      const head = size * 0.42;
      return (
        <>
          <View style={{ width: stroke, height: size * 0.78, backgroundColor: color, borderRadius: stroke }} />
          <View
            style={{
              position: 'absolute',
              width: head,
              height: head,
              borderColor: color,
              borderTopWidth: up ? stroke : 0,
              borderLeftWidth: up ? stroke : 0,
              borderBottomWidth: up ? 0 : stroke,
              borderRightWidth: up ? 0 : stroke,
              transform: [{ rotate: '45deg' }, { translateY: up ? -size * 0.13 : size * 0.13 }],
            }}
          />
        </>
      );
    }

    case 'arrow-right':
    case 'send': {
      const head = size * 0.4;
      return (
        <>
          <View style={{ width: size * 0.76, height: stroke, backgroundColor: color, borderRadius: stroke }} />
          <View
            style={{
              position: 'absolute',
              width: head,
              height: head,
              borderColor: color,
              borderTopWidth: stroke,
              borderRightWidth: stroke,
              transform: [{ rotate: '45deg' }, { translateX: size * 0.13 }],
            }}
          />
        </>
      );
    }

    case 'check':
      return (
        <View
          style={{
            width: size * 0.62,
            height: size * 0.34,
            borderColor: color,
            borderBottomWidth: stroke,
            borderLeftWidth: stroke,
            transform: [{ rotate: '-45deg' }, { translateY: -size * 0.06 }],
          }}
        />
      );

    case 'clock':
      return (
        <View style={[styles.circle(size, stroke, color), { alignItems: 'center', justifyContent: 'center' }]}>
          <View
            style={{
              position: 'absolute',
              width: stroke,
              height: size * 0.26,
              backgroundColor: color,
              borderRadius: stroke,
              transform: [{ translateY: -size * 0.13 }],
            }}
          />
          <View
            style={{
              position: 'absolute',
              width: size * 0.2,
              height: stroke,
              backgroundColor: color,
              borderRadius: stroke,
              transform: [{ translateX: size * 0.1 }],
            }}
          />
        </View>
      );

    case 'lock':
      return (
        <>
          <View
            style={{
              width: size * 0.4,
              height: size * 0.34,
              borderColor: color,
              borderWidth: stroke,
              borderBottomWidth: 0,
              borderTopLeftRadius: size * 0.2,
              borderTopRightRadius: size * 0.2,
              transform: [{ translateY: stroke }],
            }}
          />
          <View
            style={{
              width: size * 0.74,
              height: size * 0.44,
              borderRadius: size * 0.12,
              borderColor: color,
              borderWidth: stroke,
            }}
          />
        </>
      );

    case 'person':
      // Pleine, et non détourée : à 20 px, une tête cerclée au-dessus
      // d'un arc cerclé se lit comme deux formes sans rapport.
      return (
        <>
          <View
            style={{
              width: size * 0.38,
              height: size * 0.38,
              borderRadius: size * 0.19,
              backgroundColor: color,
              transform: [{ translateY: -size * 0.21 }],
            }}
          />
          <View
            style={{
              width: size * 0.72,
              height: size * 0.3,
              backgroundColor: color,
              borderTopLeftRadius: size * 0.36,
              borderTopRightRadius: size * 0.36,
              transform: [{ translateY: size * 0.27 }],
            }}
          />
        </>
      );

    case 'home':
      return (
        <>
          <View
            style={{
              width: 0,
              height: 0,
              borderLeftWidth: size * 0.42,
              borderRightWidth: size * 0.42,
              borderBottomWidth: size * 0.34,
              borderLeftColor: 'transparent',
              borderRightColor: 'transparent',
              borderBottomColor: color,
              transform: [{ translateY: -size * 0.26 }],
            }}
          />
          <View
            style={{
              width: size * 0.62,
              height: size * 0.44,
              borderColor: color,
              borderWidth: stroke,
              borderTopWidth: 0,
              transform: [{ translateY: size * 0.19 }],
            }}
          />
        </>
      );

    case 'card':
      return (
        <View
          style={{
            width: size * 0.86,
            height: size * 0.62,
            borderRadius: size * 0.16,
            borderColor: color,
            borderWidth: stroke,
            overflow: 'hidden',
          }}
        >
          <View style={{ height: stroke * 1.4, backgroundColor: color, marginTop: size * 0.14 }} />
        </View>
      );

    // Le glyphe typographique est ici plus lisible à petite taille que
    // n'importe quelle forme composée de bordures.
    case 'help':
    case 'alert':
      return (
        <View style={[styles.circle(size, stroke, color), { alignItems: 'center', justifyContent: 'center' }]}>
          <Text style={{ color, fontSize: size * 0.56, fontWeight: '700', lineHeight: size * 0.72 }}>
            {name === 'help' ? '?' : '!'}
          </Text>
        </View>
      );

    case 'plus':
      return (
        <>
          <View style={{ position: 'absolute', width: size * 0.72, height: stroke, backgroundColor: color, borderRadius: stroke }} />
          <View style={{ position: 'absolute', width: stroke, height: size * 0.72, backgroundColor: color, borderRadius: stroke }} />
        </>
      );
  }
}

const styles = {
  // Styles dépendants des props : calculés, donc hors StyleSheet.create.
  circle: (size: number, stroke: number, color: string): ViewStyle => ({
    width: size,
    height: size,
    borderRadius: size / 2,
    borderWidth: stroke,
    borderColor: color,
  }),
};
