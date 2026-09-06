plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    // KSP génère le code de Room (implémentations des DAO et de la base).
    alias(libs.plugins.ksp)
}

// Le plugin Google Services n'est appliqué que si le fichier de configuration
// Firebase est présent. Sans lui, il ferait échouer la compilation ; avec cette
// garde, le projet se compile et s'exécute tel quel (mode local seul), et la
// synchronisation distante s'active dès que google-services.json est déposé
// dans app/. Voir README, section « Base distante Firebase ».
val configurationFirebase = file("google-services.json")
if (configurationFirebase.exists()) {
    apply(plugin = "com.google.gms.google-services")
}

android {
    namespace = "com.example.cooperativeagricole"
    compileSdk = 36

    defaultConfig {
        applicationId = "com.example.cooperativeagricole"
        minSdk = 24
        targetSdk = 36
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    kotlinOptions {
        jvmTarget = "11"
    }
    buildFeatures {
        // Le View Binding remplace findViewById : chaque layout expose une
        // classe de liaison typée, ce qui évite les erreurs d'identifiant.
        viewBinding = true
    }
}

// Le schéma de la base est exporté dans app/schemas : il sert de trace des
// versions successives et permet d'écrire des tests de migration.
ksp {
    arg("room.schemaLocation", "$projectDir/schemas")
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.activity)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.androidx.recyclerview)

    // MVVM
    implementation(libs.androidx.lifecycle.viewmodel.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.livedata.ktx)

    // Room
    implementation(libs.androidx.room.runtime)
    implementation(libs.androidx.room.ktx)
    ksp(libs.androidx.room.compiler)

    // Coroutines
    implementation(libs.kotlinx.coroutines.android)

    // Firebase — base distante. La dépendance est toujours compilée ; c'est
    // l'absence de google-services.json qui décide, à l'exécution, que
    // l'application reste en local seul.
    implementation(platform(libs.firebase.bom))
    implementation(libs.firebase.firestore)

    testImplementation(libs.junit)
    testImplementation(libs.kotlinx.coroutines.test)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.room.testing)
    androidTestImplementation(libs.kotlinx.coroutines.test)
}
